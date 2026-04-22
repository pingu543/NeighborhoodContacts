import React, { useEffect, useState } from "react";


export type Contact = {
    id: string;
    contactName: string;
    contactEmail?: string;
    contactNumber?: string;
    AboutMe?: string;
    NewPassword?: string;
    isVisible?: boolean;
    isAcitive?: boolean;
    isAdmin?: boolean;
};

type Props = {
    preferAdmin?: boolean;
    pageSize?: number;
    onSelect?: (id: string) => void;
    onError?: (msg: string) => void;
};


function ContactDetails() {
  return (
    <p>Hello world!</p>
  );
}

export default ContactDetails;