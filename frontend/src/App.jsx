import { useEffect, useState } from 'react'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import ModuleCard from './components/ModuleCard'
import './App.css'
import './index.css'

function App() {
  const [connection, setConnection] = useState(null)
  const modules = ["NewTask", "Worker", "Send", "Receive", "Emitlog", "Receivelogs"]

  useEffect(() => {
    const newConnection = new HubConnectionBuilder()
      .withUrl('http://localhost:4000/consolehub')
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect()
      .build()

    newConnection.start()
      .then(() => {
        console.log("Connected to SignalR Hub");
        setConnection(newConnection);
      })
      .catch(e => console.log('Connection failed: ', e));

    return () => {
      newConnection.stop();
    };
  }, [])

  return (
    <div className="app-container">
      <header className="app-header">
        <h1>RabbitMQ Testing Dashboard</h1>
        <div className="header-status">
          SignalR: {connection ? <span className="text-green">Connected 🟢</span> : <span className="text-red">Disconnected 🔴</span>}
        </div>
      </header>

      <main className="dashboard-grid">
        {modules.map(mod => (
          <ModuleCard key={mod} moduleName={mod} connection={connection} />
        ))}
      </main>
    </div>
  )
}

export default App
