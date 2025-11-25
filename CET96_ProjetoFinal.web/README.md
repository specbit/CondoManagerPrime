# CondoManagerPrime

![C#](https://img.shields.io/badge/C%23-239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-512BD4.svg?style=for-the-badge&logo=dotnet&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/EF_Core-512BD4.svg?style=for-the-badge&logo=database&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927.svg?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-5C2D91.svg?style=for-the-badge&logo=signalr&logoColor=white)

CondoManagerPrime is a complete Software as a Service (SaaS) platform designed to streamline all aspects of condominium management. Built on ASP.NET Core with a clean, repository-based architecture, this application provides a robust solution for managing companies, condominiums, units, staff, and residents.

The system features a multi-tiered user role hierarchy, including:
- **Platform Administrator:** Full oversight of the entire system, including company and user management.
- **Company Administrator:** Manages multiple condominium properties.
- **Condominium Manager:** Oversees the day-to-day operations of a single condominium, including staff and unit management.
- **Condominium Staff:** Employees with specific operational roles within a condominium.
- **Unit Owner:** Resident users with access to personal unit information, messaging, and reporting tools.

## Key Features
- **Role-Based Dashboards:** Each user role has a tailored dashboard displaying relevant information and actions.
- **Cascade Activation/Deactivation:** Platform Administrators can lock or unlock entire company accounts, including all subordinate users, with a single click.
- **User Management:** Secure registration, login, password recovery, and role-based permissions powered by ASP.NET Core Identity.
- **Dynamic Navigation:** A smart UI that adapts to the logged-in user's permissions, ensuring a clean and relevant experience.
- **Email Notifications:** Integrated email service for critical actions such as account confirmation and status changes.
- **Real-Time Messaging:** SignalR-powered conversation system enabling instant communication between managers, staff, and unit owners. Includes workflow statuses (Pending, In Progress, Resolved, Closed), role-based visibility, unread indicators, and persistent history.

## Technology Stack
- **Backend:** ASP.NET Core MVC, C#, Entity Framework Core  
- **Frontend:** Razor Pages, HTML, CSS, Bootstrap  
- **Database:** SQL Server  
- **Authentication:** ASP.NET Core Identity  
- **Real-Time Communication:** SignalR for messaging, unread indicators, and live updates  

## Architecture Overview
CondoManagerPrime follows a clean, modular, and maintainable structure designed for scalability:

- **Domain-Driven Entities:** Companies, Condominiums, Units, Users, Conversations, Messages.
- **Repository Pattern:** All database access is abstracted through repositories, improving testability and separation of concerns.
- **Service Layer:** Business logic for user management, messaging workflows, activation cascades, and permissions enforcement.
- **Controllers & Razor Views:** MVC architecture handling UI rendering, request handling, and role-based access control.
- **SignalR Hub:** Dedicated hub for broadcasting new messages, updating statuses, and managing user connection presence.
- **Modular Boundaries:** Authentication, Messaging, Condominium Management, and Administration are cleanly separated.
