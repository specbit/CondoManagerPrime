# CondoManagerPrime

CondoManagerPrime is a complete Software as a Service (SaaS) platform designed to streamline all aspects of condominium management. Built on ASP.NET Core with a clean, repository-based architecture, this application provides a robust solution for managing companies, condominiums, units, staff, and residents.

The system features a multi-tiered user role hierarchy, including:
- **Platform Administrator:** Full oversight of the entire system, including company and user management.
- **Company Administrator:** Manages multiple condominium properties.
- **Condominium Manager:** Oversees the day-to-day operations of a single condominium, including staff and unit management.
- **Condominium Staff:** Employees with specific roles within a condominium.

## Key Features
- **Role-Based Dashboards:** Each user role has a tailored dashboard displaying relevant information and actions.
- **Cascade Activation/Deactivation:** Platform Administrators can lock or unlock entire company accounts, including all subordinate users, with a single click.
- **User Management:** Secure registration, login, password recovery, and role-based permissions powered by ASP.NET Core Identity.
- **Dynamic Navigation:** A smart UI that adapts to the logged-in user's permissions, ensuring a clean and relevant experience.
- **Email Notifications:** Integrated email service for critical actions like account confirmation and status changes.

## Technology Stack
- **Backend:** ASP.NET Core MVC, C#, Entity Framework Core
- **Frontend:** Razor Pages, HTML, CSS, Bootstrap
- **Database:** SQL Server
- **Authentication:** ASP.NET Core Identity