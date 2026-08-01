namespace OpenCMIS.Cypress
{
    /// <summary>
    ///     Delegate for event handler to handle the device change events
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
    public delegate void EZUSBHandler(object sender, EventArgs e);

    /// <summary>
    ///     Class of EZ USB event arguments
    /// </summary>
    public class EZUSBEventArgs : EventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="EZUSBEventArgs" /> class.
        /// </summary>
        /// <param name="vendorID">The vendor id.</param>
        /// <param name="productId">The product id.</param>
        /// <param name="serialID">The serial id.</param>
        public EZUSBEventArgs(int vendorID, int productId, string serialID)
        {
            VendorID  = vendorID;
            ProductID = productId;
            SerialID  = serialID;
        }

        /// <summary>
        ///     Gets the vendor id.
        /// </summary>
        /// <value>
        ///     The vendor id.
        /// </value>
        public int VendorID { get; }

        /// <summary>
        ///     Gets the product id.
        /// </summary>
        /// <value>
        ///     The product id.
        /// </value>
        public int ProductID { get; }

        /// <summary>
        ///     Gets the serial id.
        /// </summary>
        /// <value>
        ///     The serial id.
        /// </value>
        public string SerialID { get; }
    }
}