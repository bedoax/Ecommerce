Certainly! Here’s the updated README file with the implementation details changed from `PasswordEncryptionService` to `PasswordEncryption`:

---

# Ecommerce Project

Welcome to the Ecommerce Project! This web application allows users to browse, purchase, and manage their orders of various products. It includes features for user authentication, payment processing, and account management.

## Features

### User Authentication
- **Login/Logout**: Secure login and logout functionality using cookies.
- **Registration**: User registration with validation and account creation.
- **Account Management**: Change username, email, and password.
- **Custom Password Encryption**: Secure storage of passwords using custom encryption methods.

### Product Management
- **Browse Products**: View products with detailed information including price and images.
- **Search and Filter**: Search for products and filter by categories.

### Shopping Cart
- **Add/Remove Items**: Add items to the cart and remove them as needed.
- **View Cart**: View items in the cart, including total price.
- **Checkout**: Purchase items in the cart using Stripe for payment processing.

### Order Management
- **View Orders**: View a list of orders with status updates and tracking information.
- **Track Orders**: Track the status of individual orders.

### Admin Features
- **User Management**: Admins can manage user accounts and roles.
- **Order Management**: Admins can view and update order statuses.

## Tech Stack

- **Frontend**: HTML, CSS, JavaScript, Bootstrap
- **Backend**: ASP.NET Core MVC
- **Database**: SQL Server with Entity Framework Core
- **Payment Processing**: Stripe
- **Password Encryption**: Custom encryption methods for securing passwords

## Installation

1. **Clone the Repository**
   ```bash
   git clone https://github.com/yourusername/ecommerce-project.git
   cd ecommerce-project
   ```

2. **Set Up the Database**
   - Ensure you have SQL Server installed.
   - Update the connection string in `appsettings.json` with your SQL Server details.

3. **Configure Stripe**
   - Update the Stripe API keys in `appsettings.json` with your own test or live keys.

4. **Install Dependencies**
   ```bash
   dotnet restore
   ```

5. **Run the Application**
   ```bash
   dotnet run
   ```

   Navigate to `http://localhost:5000` to view the application.

## Custom Password Encryption

This project includes custom encryption for securely storing user passwords. The custom encryption method ensures that passwords are stored in a hashed and salted format, adding an extra layer of security.

### Implementation Details

- Passwords are hashed using a custom algorithm during registration.
- During login, the provided password is hashed and compared with the stored hash.
- The custom encryption logic is implemented in the `PasswordEncryption` class.

## Usage

- **Register**: Create an account to start using the application.
- **Login**: Log in to manage your account and make purchases.
- **Browse Products**: Explore and search for products.
- **Manage Orders**: Track and view your orders.

## Contributing

Feel free to fork the repository, create a branch, and submit a pull request. Please ensure that your contributions align with the project's coding standards and include appropriate tests.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

## Contact

For any questions or support, please contact [your-email@example.com](mailto:abdelrahmanneehad@gmail.com).

---
