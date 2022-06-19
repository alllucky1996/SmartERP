﻿namespace Smartstore.Net.Mail
{
    /// <summary>
    /// Mail service abstraction. Responsible for connecting to SMTP hosts.
    /// </summary>
    public partial interface IMailService
    {
        /// <summary>
        /// Connects to the SMTP host with data provided by <paramref name="account"/>
        /// </summary>
        /// <param name="account">The mail account</param>
        /// <param name="timeout">Connection timeout in milliseconds.</param>
        /// <returns>An SMTP client instance that can send mails.</returns>
        ISmtpClient Connect(IMailAccount account, int timeout = 1000);

        /// <summary>
        /// Connects to the SMTP host with data provided by <paramref name="account"/>
        /// </summary>
        /// <param name="account">The mail account</param>
        /// <param name="timeout">Connection timeout in milliseconds.</param>
        /// <returns>An SMTP client instance that can send mails.</returns>
        Task<ISmtpClient> ConnectAsync(IMailAccount account, int timeout = 1000);

        /// <summary>
        /// Saves a mail message to the specified pickup directory on local disk.
        /// </summary>
        /// <param name="pickupDirectory">The physical pickup directory path.</param>
        /// <param name="message">Mail message to save to disk.</param>
        Task SaveAsync(string pickupDirectory, MailMessage message);
    }
}