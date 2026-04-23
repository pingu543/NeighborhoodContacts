using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NeighborhoodContacts.Server.Data;
using NeighborhoodContacts.Server.Data.Entities;
using System;
using System.Data;
using Spire.Pdf;
using Spire.Pdf.Graphics;
using Spire.Pdf.Tables;
using System.Drawing;
using System.Numerics;
using System.Text.Json.Serialization;

namespace NeighborhoodContacts.Server.Features
{
    // Endpoint registrations for contacts list (user-scoped)
    public static class ContactEndpoints
    {
        public static void MapContactEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/contacts", GetAllContacts)
               .WithName("GetContacts")
               .WithTags("Contacts");

            app.MapGet("/api/contacts/download-pdf", GetAllContactsPDF)
                .WithName("GetContactsPDF")
                .WithTags("Contacts");
        }

        // Get contacts list for the contact list page (user view).
        // Non-admin callers get only active and visible contacts in their property group.
        private static async Task<IResult> GetAllContacts(ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
        {
            // find current user's group
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var uid)) return Results.Forbid();

            var currentUser = await db.Users
                                      .AsNoTracking()
                                      .Include(u => u.Property)
                                      .FirstOrDefaultAsync(u => u.Id == uid, ct);

            var requestedGroup = currentUser?.Property?.PropertyGroupId;
            if (requestedGroup == null) return Results.Ok(new List<ContactListItemDto>());

            var rg = requestedGroup.Value;

            var list = await db.Users
                .AsNoTracking()
                .Include(u => u.Property)
                .Where(u => u.IsActive && u.IsVisible && u.Property != null && u.Property.PropertyGroupId == rg)
                .Select(u => new ContactListItemDto
                {
                    Id = u.Id,
                    ContactName = u.ContactName,
                    ContactNumber = u.ContactNumber,
                    ContactEmail = u.ContactEmail,
                    PropertyAddress = u.Property != null ? u.Property.Address : null
                })
                .ToListAsync(ct);

            return Results.Ok(list);
        }

        private static async Task<IResult> GetAllContactsPDF(ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
        {
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var uid)) return Results.Forbid();

            var currentUser = await db.Users
                                      .AsNoTracking()
                                      .Include(u => u.Property)
                                      .FirstOrDefaultAsync(u => u.Id == uid, ct);

            var requestedGroup = currentUser?.Property?.PropertyGroupId;
            if (requestedGroup == null)
            {
                // Create a small PDF explaining why there are no contacts so the client receives a PDF
                PdfDocument emptyPdf = new PdfDocument();
                PdfPageBase emptyPage = emptyPdf.Pages.Add();
                string message = "No contacts available. You are not assigned to a property or property group.";
                PdfFont msgFont = new PdfFont(PdfFontFamily.Helvetica, 12f);
                emptyPage.Canvas.DrawString(message, msgFont, PdfBrushes.Black, 20, 40);
                using var ms = new System.IO.MemoryStream();
                emptyPdf.SaveToStream(ms);
                return Results.File(ms.ToArray(), "application/pdf", "contacts.pdf");
            }

            var rg = requestedGroup.Value;

            var list = await db.Users
                .AsNoTracking()
                .Include(u => u.Property)
                .Where(u => u.IsActive && u.IsVisible && u.Property != null && u.Property.PropertyGroupId == rg)
                .Select(u => new ContactListItemDto
                {
                    ContactName = u.ContactName,
                    ContactNumber = u.ContactNumber,
                    ContactEmail = u.ContactEmail,
                    PropertyAddress = u.Property != null ? u.Property.Address : null
                })
                .ToListAsync(ct);

            PdfDocument pdf = new PdfDocument();
            PdfPageBase page = pdf.Pages.Add();
            string title = "All Contacts";
            PdfFont titleFont = new PdfFont(PdfFontFamily.Helvetica, 20f, PdfFontStyle.Bold);
            SizeF titleSize = titleFont.MeasureString(title);
            float titleX = (page.Canvas.ClientSize.Width - titleSize.Width) / 2;
            page.Canvas.DrawString(title, titleFont, PdfBrushes.Black, titleX, 20);

            PdfTable pdfTable = new PdfTable();
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("Name");
            dataTable.Columns.Add("Number");
            dataTable.Columns.Add("Email");
            dataTable.Columns.Add("Address");

            foreach(ContactListItemDto item in list)
            {
                dataTable.Rows.Add([item.ContactName, item.ContactNumber, item.ContactEmail, item.PropertyAddress]);
            }
            pdfTable.DataSource = dataTable;
            pdfTable.Style.ShowHeader = true;

            pdfTable.Style.CellPadding = 2;
            pdfTable.Style.HeaderStyle.StringFormat = new PdfStringFormat(PdfTextAlignment.Center);

            pdfTable.Draw(page, new PointF(0, 60));

            MemoryStream stream = new MemoryStream();

            pdf.SaveToStream(stream);
 
            return Results.File(stream, "application/pdf", "ContactList.pdf");
        }
    }



    public sealed class ContactListItemDto
    {
        public Guid Id { get; init; }
        public string ContactName { get; init; } = string.Empty;
        public string? ContactNumber { get; init; }
        public string? ContactEmail { get; init; }
        public string? PropertyAddress { get; init; }
    }
}
