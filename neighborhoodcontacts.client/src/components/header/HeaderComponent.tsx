import AccountComponent from '../account/AccountComponent';

function HeaderComponent() {
  return (
    <nav className="navbar navbar-light bg-white border-bottom sticky-top">
      <div className="container-fluid">
        <span className="navbar-brand h4 d-block mb-1">Neighborhood Contacts</span>
        <div>
          <AccountComponent />
        </div>
      </div>
    </nav>
  );
}

export default HeaderComponent;