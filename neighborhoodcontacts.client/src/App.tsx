import './App.css';
import ContactListPage from './pages/contact-list/ContactListPage';
import HeaderComponent from './components/header/HeaderComponent';

function App() {
  return (
    <div className="app-root">
      <HeaderComponent />
      <main className="container py-4">
        <ContactListPage />
      </main>
    </div>
  );
}

export default App;