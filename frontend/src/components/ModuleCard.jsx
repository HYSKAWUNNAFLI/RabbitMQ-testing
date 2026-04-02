import React, { useEffect, useState, useRef } from 'react';

const ModuleCard = ({ moduleName, connection }) => {
  const [logs, setLogs] = useState([]);
  const [isRunning, setIsRunning] = useState(false);
  const logsEndRef = useRef(null);
  
  // URL to the ASP.NET Core API
  const API_BASE = 'http://localhost:4000/api/module';

  useEffect(() => {
    if (connection) {
      const handleReceiveLog = (targetModule, logMessage) => {
        if (targetModule === moduleName) {
          setLogs((prev) => [...prev, logMessage]);
          if (logMessage.includes("Module started") || logMessage.includes("Project")) {
            setIsRunning(true);
          }
          if (logMessage.includes("Module stopped") || logMessage.includes("Exited")) {
            setIsRunning(false);
          }
        }
      };
      
      connection.on("ReceiveLog", handleReceiveLog);

      return () => {
        connection.off("ReceiveLog", handleReceiveLog);
      };
    }
  }, [connection, moduleName]);

  useEffect(() => {
    logsEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [logs]);

  const handleStart = async () => {
    try {
      await fetch(`${API_BASE}/start/${moduleName}`, { method: 'POST' });
    } catch (e) {
      console.error(e);
      setLogs((prev) => [...prev, `[UI Error] Could not start: ${e.message}`]);
    }
  };

  const handleStop = async () => {
    try {
      await fetch(`${API_BASE}/stop/${moduleName}`, { method: 'POST' });
    } catch (e) {
      console.error(e);
    }
  };

  const clearLogs = () => setLogs([]);

  return (
    <div className="module-card glass">
      <div className="module-header">
        <h2>{moduleName}</h2>
        <div className="status-indicator">
          <span className={`dot ${isRunning ? 'running' : 'stopped'}`}></span>
          <span className="status-text">{isRunning ? 'Running' : 'Stopped'}</span>
        </div>
      </div>
      
      <div className="module-actions">
        <button onClick={handleStart} disabled={isRunning} className="btn btn-start">▶ Start</button>
        <button onClick={handleStop} disabled={!isRunning} className="btn btn-stop">⏹ Stop</button>
        <button onClick={clearLogs} className="btn btn-clear">Clear</button>
      </div>

      <div className="module-terminal">
        {logs.length === 0 ? (
          <div className="empty-log">Awaiting logs...</div>
        ) : (
          logs.map((log, index) => (
            <div key={index} className="log-entry">{log}</div>
          ))
        )}
        <div ref={logsEndRef} />
      </div>
    </div>
  );
};

export default ModuleCard;
