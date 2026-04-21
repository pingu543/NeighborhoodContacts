import { useState, useEffect } from 'react'

interface Property {
    id: string,
    address: string,
    PropertyGroupId: string,
}

// A long bar with buttons:
// Property Group.
// - Dropdown to select property group (shows the name of the group).
//  The first item in the drop down is "All contacts" which shows all contacts and properties (will not include a property group in the endpoint call).
//  Selecting a property group will show the contacts for that group and the properties in that group.
// Add contact. When clicked, it opens a form with the following fields:
// - Email (text input as username)
// - Address (dropdown)
// - Confirm button. (shows the auto generated password with copy button).
// - Cancel button
// Add property.
// - Text field for full address.
// - Confirm button
// - Cancel button
// Edit property.
// - Dropdown to select property to edit.
// - Text field for full address (prefilled with the current address).
// - Confirm button
// - Delete button
// - Cancel button

function AdminControl() {
    const [propertyList, setPropertyList] = useState<Property[]>();

    useEffect(() => {
        const fetchProperties = async () => {
            try {
                const response = await fetch("/api/properties", {
                    method: "GET",
                    credentials: 'include',
                    headers: { "Content-Type": "applications/json" }
                });
                const data = await response.json();
                setPropertyList(data);
                console.log(data);
            }
            catch (e) {
                console.error("Error getting properties: " + e);
            }
        }
        fetchProperties();
    }, [])


    return (
    <div>
      <p>Properties</p>
      <select>
         <option value="None">None</option>
      </select>
    </div>
  );
}

export default AdminControl;