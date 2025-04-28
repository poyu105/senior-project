import AppRoutes from "./AppRoutes"
import Header from "./components/Header"
import Navbar from "./components/Navbar"

function App() {
  return (
    <>
      <Navbar/>
      <div className="mx-5">
        <Header/>
        <AppRoutes/>
      </div>
    </>
  )
}

export default App
