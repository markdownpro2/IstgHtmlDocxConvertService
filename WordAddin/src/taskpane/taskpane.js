let socket = null;
let saveInterval = null;
let lastContent = "";
let sessionId = "";
let token = "";
const autoSaveInterval = 20000;
let changeTimeout = null;
let count = 0;

import { websocketUrl, actions } from "./conf";

Office.onReady(async (info) => {
  if (info.host === Office.HostType.Word) {
    document.getElementById("sideload-msg").style.display = "flex";
    document.getElementById("app-body").style.display = "flex";

    initializeAddin();

    window.addEventListener("beforeunload", async () => {
      saveDocumentContent(true);
      sendMessage({ Origin: "word", Action: actions.endSession, SessionId: sessionId });
      socket?.close();
    });
  }
});

async function initializeAddin() {
  try {
    await Word.run(async (context) => {
      context.document.properties.customProperties.load("items");
      await context.sync();

      const sessionIdProp = context.document.properties.customProperties.items.find(
        (p) => p.key === "sessionId"
      );
      const tokenProp = context.document.properties.customProperties.items.find(
        (p) => p.key === "token"
      );

      if (sessionIdProp) {
        sessionId = sessionIdProp.value;
        console.log("sessionId found:", sessionId);
      } else {
        throw new Error("sessionId not found in custom properties.");
      }

      if (tokenProp) {
        token = tokenProp.value;
        console.log("token found:", token);
      } else {
        throw new Error("token not found in custom properties.");
      }

      await setupWebSocket();
      attachChangeHandler();
      startAutoSave();
    });
  } catch (err) {
    console.error("Error in initializeAddin:", err);
    document.getElementById("sideload-msg").innerHTML =
      `<h2 class="error">Error in initializeAddin</h2><p>${err.message}</p>`;
    document.getElementById("app-body").style.display = "none";
    // throw err;
  }
}

async function setupWebSocket() {
  return new Promise((resolve, reject) => {
    socket = new WebSocket(websocketUrl);

    socket.onopen = () => {
      console.log("WebSocket connected");
      resolve();
    };

    socket.onerror = (err) => {
      console.error("WebSocket error:", err);
      reject(err);
    };

    socket.onmessage = (event) => {
      try {
        const message = JSON.parse(event.data);
        console.log("WebSocket message received:", message);

        if (message.Action === actions.endSession && message.Success) {
          const reason = "Session Ended";
          const description = "The editing session has been terminated by client.";

          document.getElementById("sideload-msg").innerHTML = `
            <h2 style="color: red;">${reason}</h2>
            <p>${description}</p>
            <p>Please close this document.</p>
        `;
          document.getElementById("app-body").style.display = "none";
          // Clean up
          console.log("End session");
          clearInterval(saveInterval);
          Office.context.document.removeHandlerAsync(Office.EventType.DocumentSelectionChanged);
          socket?.close();
          return;
        }

        if (message.Action === actions.sessionClosed && message.Success) {
          const reason = "Session Closed";
          const description = "The editing session has been terminated by server.";

          document.getElementById("sideload-msg").innerHTML = `
            <h2 style="color: red;">${reason}</h2>
            <p>${description}</p>
            <p>Please close this document.</p>
        `;
          document.getElementById("app-body").style.display = "none";
          // Clean up
          console.log("Closed session");
          Office.context.document.removeHandlerAsync(Office.EventType.DocumentSelectionChanged);
          socket?.close();
          return;
        }

        if (!message.Success) {
          console.warn("Server error:", message.Content);
          document.getElementById("sideload-msg").innerHTML = `
            <h2 style="color: red;">Something Went Wrong</h2>
            <p>You can close this document.</p>
        `;
          document.getElementById("app-body").style.display = "none";
          // Clean up
          console.log("Something Wen't wrong");
          Office.context.document.removeHandlerAsync(Office.EventType.DocumentSelectionChanged);
          sendMessage({ Origin: "word", Action: "end-session", SessionId: sessionId });
          socket?.close();
        }
      } catch (e) {
        console.error("Failed to parse WebSocket message:", e);
      }
    };

    socket.onclose = () => {
      console.warn("WebSocket closed");
    };
  });
}

function startAutoSave() {
  if (saveInterval) clearInterval(saveInterval);
  saveInterval = setInterval(saveDocumentContent, autoSaveInterval);
}

// ✅ New: Listen to document selection change (acts as an edit proxy)
function attachChangeHandler() {
  Office.context.document.addHandlerAsync(
    Office.EventType.DocumentSelectionChanged,
    () => {
      if (changeTimeout) clearTimeout(changeTimeout);
      changeTimeout = setTimeout(() => {
        console.log("Change detected, attempting to save...");
        saveDocumentContent();
      }, 100);
    },
    (result) => {
      if (result.status === Office.AsyncResultStatus.Succeeded) {
        console.log("Change handler attached successfully.");
      } else {
        console.error("Failed to attach change handler:", result.error);
      }
    }
  );
}

async function saveDocumentContent(isClosing = false) {
  if (!sessionId || socket?.readyState !== WebSocket.OPEN) return;

  try {
    await Word.run(async (context) => {
      const body = context.document.body;
      const ooxml = body.getOoxml();
      await context.sync();

      const currentContent = ooxml.value;

      if (currentContent === lastContent) {
        if (!isClosing) console.log("No changes to save.");
        return;
      }

      lastContent = currentContent;
      console.log("before Saving document content...");

      if (count == 0 || count == 1) {
        sendMessage({
          Origin: "word",
          Token: token,
          Action: actions.updateOoxml,
          SessionId: sessionId,
          PayloadType: "ooxml",
          Content: currentContent,
        });
      } else {
        sendMessage({
          Origin: "word",
          Action: actions.updateOoxml,
          SessionId: sessionId,
          PayloadType: "ooxml",
          Content: currentContent,
        });
      }
      count++;
      const time = new Date().toLocaleTimeString();

      document.getElementById("sideload-msg").innerHTML = `
        <h2 class="success">Last Saved ${time}</h2>
        <p>Document content has been saved successfully.</p>
      `;
      document.getElementById("app-body").style.display = "none";

      console.log("Saved document content");
    });
  } catch (err) {
    console.error("Error during save:", err);
  }
}

function sendMessage(message) {
  if (socket && socket.readyState === WebSocket.OPEN) {
    socket.send(JSON.stringify(message));
  } else {
    console.warn("WebSocket not open. Message not sent:", message);
  }
}
