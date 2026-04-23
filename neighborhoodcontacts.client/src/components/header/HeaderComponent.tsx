import AccountComponent from '../account/AccountComponent';
import { Link } from 'react-router-dom';

// Contains title and account management.

function HeaderComponent() {
    return (
        <nav className="navbar">
            <div className="container-fluid">
                <Link to="/" className="navbar-brand">Neighborhood Contacts</Link>
                <div>
                    <AccountComponent />
                </div>
            </div>
        </nav>
    );
}

export default HeaderComponent;