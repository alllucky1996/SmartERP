﻿using System.ComponentModel;
using Smartstore.Collections;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class CheckoutState : INotifyPropertyChanged
    {
        public static string CheckoutStateSessionKey => ".Smart.CheckoutState";

        private string _paymentSummary;
        private bool _isPaymentSelectionSkipped;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The payment summary as displayed on the checkout confirmation page
        /// </summary>
        public string PaymentSummary 
        {
            get => _paymentSummary;
            set
            {
                if (_paymentSummary != value)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PaymentSummary)));
                }

                _paymentSummary = value;
            }
        }

        /// <summary>
        /// Indicates whether the payment method selection page was skipped
        /// </summary>
        public bool IsPaymentSelectionSkipped
        {
            get => _isPaymentSelectionSkipped;
            set
            {
                if (_isPaymentSelectionSkipped != value)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPaymentSelectionSkipped)));
                }

                _isPaymentSelectionSkipped = value;
            }
        }

        /// <summary>
        /// Use this dictionary for any custom data required along checkout flow
        /// </summary>
        public ObservableDictionary<string, object> CustomProperties { get; set; } = new();

        /// <summary>
        /// The payment data entered on payment method selection page
        /// </summary>
        public ObservableDictionary<string, object> PaymentData { get; set; } = new();
    }
}