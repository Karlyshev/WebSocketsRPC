namespace WebSocketsRPC 
{
    #region Description
    /// <summary>
    /// Filter of WebSocket clients for sending messages
    /// </summary>
    #endregion Description
    public enum SendToConfigurationType : byte
    {
        #region Description
        /// <summary>
        /// Filter "ALL CLIENTS". Send message to all clients
        /// </summary>
        #endregion Description
        All = 0,
        #region Description
        /// <summary>
        /// Filter "CALLER". Send message to only caller
        /// </summary>
        #endregion Description
        Caller = 1,
        #region Description
        /// <summary>
        /// Filter "OTHER CLIENTS". Send message to all clients except caller
        /// </summary>
        #endregion Description
        Others = 2,
        #region Description
        /// <summary>
        /// Filter "EXCEPT CLIENTS". Send message to all clients except clients specified in additionalData field
        /// </summary>
        #endregion Description
        ExceptClients = 3,
        #region Description
        /// <summary>
        /// Filter "SPECIFIED CLIENT". Send message to only client specified in additionalData field
        /// </summary>
        #endregion Description
        SpecifiedClient = 4,
        #region Description
        /// <summary>
        /// Filter "SPECIFIED CLIENTS". Send message to only clients specified in additionalData field
        /// </summary>
        #endregion Description
        SpecifiedClients = 5
    }
}