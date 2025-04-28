import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App.jsx'
import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap/dist/js/bootstrap.bundle.min.js';
import { BrowserRouter } from 'react-router-dom';
import { LoadingProvider } from './context/LoadingContext.jsx';
import { AuthProvider } from './context/AuthContext.jsx';

createRoot(document.getElementById('root')).render(
  <LoadingProvider>
    <AuthProvider>
      {/* <StrictMode> */}
      <BrowserRouter>
            <App />
        </BrowserRouter>
      {/* </StrictMode>, */}
    </AuthProvider>
  </LoadingProvider>
)
