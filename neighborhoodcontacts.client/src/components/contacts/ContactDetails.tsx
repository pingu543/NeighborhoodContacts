import React, { useEffect, useState } from "react";
//import type { User } from '../../context/AuthContext'

export type ContactDetails = {
    id: string;
    contactName: string;
    contactEmail?: string;
    contactNumber?: string;
    AboutMe?: string;
    NewPassword?: string;
    isVisible?: boolean;
    isActive?: boolean;
    isAdmin?: boolean;
};

type Props = {
    preferAdmin?: boolean;
    onSelect?: (id: string) => void;
    onError?: (msg: string) => void;
};

// Fetch and displlay the contact details
const ContactDetails: React.FC<Props> = ({ preferAdmin = false, onError }) => {
    const [contact, setContact] = useState<ContactDetails>();
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // add way to check if owner

    useEffect(() => {
        let mounted = true;
        (async () => {
            setLoading(true);
            setError(null);
            try {
                // Build endpoint: if preferAdmin, optionally include propertyGroupId query param
                //let endpoint = preferAdmin ? "/api/admin/contacts" : "/api/contacts";

                // use this endpoint if admin or owner
                if (preferAdmin) {
                    const endpoint = "/api/user/c976b82c-506b-4bf1-bdb6-21f60f6a5dd5"  // change id depending on context

                    const res = await fetch(endpoint, { credentials: "include" });

                    if (!res.ok) {
                        throw new Error(`Failed to load contacts (${res.status})`);
                    }

                    const details = await res.json();

                    if (!mounted) return;
                    setContact(details);
                }

                // if none of the above use this endpoint
                else
                {
                    const endpoint = "endpoint";
                }
               
               

                
               
            } catch (err) {
                const msg = err instanceof Error ? err.message : String(err);
                if (mounted) {
                    setError(msg);
                    onError?.(msg);
                }
            } finally {
                if (mounted) setLoading(false);
            }
        })();

        return () => {
            mounted = false;
        };
    }, [preferAdmin, onError]);

    if (loading) return <div>Loading contacts…</div>;
    if (error) return <div className="text-danger">Error: {error}</div>;
    if (!contact || contact == undefined) return <div>No contact found.</div>;
    
    // if admin
    if (preferAdmin == true) return( 
   
                    <div>
                       
                        <div>{contact.contactName && <button>Change Name</button>}</div>
                        <div>{contact.contactEmail && <button>Change Email</button>}</div>
                        <div>{contact.contactNumber && <button>Change Number</button>}</div>
                        <div>{contact.AboutMe && <button>Change About Me</button>}</div>
                        <div>{contact.NewPassword && <button>Change Password</button>}</div>
                        <div>{contact.isAdmin}</div>  <button>Change Admin</button>
                        <div>{contact.isVisible}</div>  <button>Change Visiblity</button>
                        <div>{contact.isActive}</div>  <button>Change Active</button>
                        <button>Save</button>

                    </div>
    )


    /*
    if (owner) return( 
   
                    <div>
                       
                        <div>{contact.contactName && <button>Change Name</button>}</div>
                        <div>{contact.contactEmail && <button>Change Email</button>}</div>
                        <div>{contact.contactNumber && <button>Change Number</button>}</div>
                        <div>{contact.AboutMe && <button>Change About Me</button>}</div>
                        <div>{contact.NewPassword && <button>Change Password</button>}</div>
                        <button>Save</button>

                    </div>
    
    
    */ 
            
    // if normal user who is NOT owner
    if (preferAdmin == false) return(
    
            <div>
               
                <div>{contact.contactName && <button>Change Name</button>}</div> 
                <div>{contact.contactEmail && <button>Change Email</button>}</div>  
                <div>{contact.contactNumber && <button>Change Number</button>}</div>  
                <div>{contact.AboutMe && <button>Change About Me</button>}</div>  
                <div>{contact.NewPassword && <button>Change Password</button> }</div> {/* need to do something else about password */  }
                <button>Save</button>

            </div>

     )
    
   
}

export default ContactDetails;