import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App.jsx'
import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap/dist/js/bootstrap.bundle.min.js';
import { BrowserRouter } from 'react-router-dom';
import { LoadingProvider } from './context/LoadingContext.jsx';
import { CartProvider } from './context/CartContext.jsx';
import { UserProvider } from './context/UserContext.jsx'

createRoot(document.getElementById('root')).render(
  <LoadingProvider>
    <UserProvider>
      <CartProvider>
        {/* <StrictMode> */}
          <BrowserRouter>
              <App />
          </BrowserRouter>
        {/* </StrictMode>, */}
      </CartProvider>
    </UserProvider>
  </LoadingProvider>
)
