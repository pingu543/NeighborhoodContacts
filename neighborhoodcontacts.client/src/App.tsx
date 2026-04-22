//import './App.css';
import React from 'react';
import { Routes, Route } from 'react-router-dom';
import ContactListPage from './pages/contact-list/ContactListPage';
import HeaderComponent from './components/header/HeaderComponent';
import { AuthProvider } from "./context/AuthProvider";

function App() {
  return (
    <AuthProvider>
      <div className="app-root">
        <HeaderComponent />
        <Routes>
          <Route path="/" element={<ContactListPage />} />
          {/* other routes */}
        </Routes>
      </div>
    </AuthProvider>
  );
}

export default App;