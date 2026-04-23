//import './App.css';
import { useEffect } from 'react';
import { Routes, Route, useLocation, useNavigate } from 'react-router-dom';
import ContactListPage from './pages/contact-list/ContactListPage';
import ContactDetailsPage from './pages/contact-details/ContactDetailsPage';
import HeaderComponent from './components/header/HeaderComponent';
import { AuthProvider } from "./context/AuthProvider";
import { useAuth } from './context/AuthContext';

function App() {
  return (
    <AuthProvider>
      <div className="app-root">
        <HeaderComponent />
        <AuthRouteGuard />
        <Routes>
          <Route path="/" element={<ContactListPage />} />
          <Route path="/contacts/:id" element={<ContactDetailsPage />} />
          {/* other routes */}
        </Routes>
      </div>
    </AuthProvider>
  );
}

function AuthRouteGuard(): null {
  const { isSignedIn } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  useEffect(() => {
    // wait until auth check completes (isSignedIn !== null)
    if (isSignedIn === false && location.pathname !== '/') {
      navigate('/', { replace: true });
    }
  }, [isSignedIn, location.pathname, navigate]);

  return null;
}

export default App;