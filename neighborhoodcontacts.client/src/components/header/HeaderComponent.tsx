import AccountComponent from '../account/AccountComponent';

// Contains title and account management.

function HeaderComponent() {
    return (
        <nav className="navbar">
            <div className="container-fluid">
                <span className="navbar-brand">Neighborhood Contacts</span>
                <div>
                    <AccountComponent />
                </div>
            </div>
        </nav>
    );
}

export default HeaderComponent;