namespace Web.Client.Models
{
    /// <summary>
    /// Represents the browser window dimensions
    /// </summary>
    public class WindowSize
    {
        /// <summary>
        /// Window width in pixels
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Window height in pixels
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Indicates if the current view is a small mobile device (width < 576px)
        /// </summary>
        public bool IsExtraSmall => Width < 576;

        /// <summary>
        /// Indicates if the current view is a small device (width ≥ 576px and width < 768px)
        /// </summary>
        public bool IsSmall => Width >= 576 && Width < 768;

        /// <summary>
        /// Indicates if the current view is a medium device (width ≥ 768px and width < 992px)
        /// </summary>
        public bool IsMedium => Width >= 768 && Width < 992;

        /// <summary>
        /// Indicates if the current view is a large device (width ≥ 992px and width < 1200px)
        /// </summary>
        public bool IsLarge => Width >= 992 && Width < 1200;

        /// <summary>
        /// Indicates if the current view is an extra large device (width ≥ 1200px and width < 1400px)
        /// </summary>
        public bool IsExtraLarge => Width >= 1200 && Width < 1400;

        /// <summary>
        /// Indicates if the current view is an extra-extra large device (width ≥ 1400px)
        /// </summary>
        public bool IsExtraExtraLarge => Width >= 1400;

        ///// <summary>
        ///// Gets a Bootstrap class for the current breakpoint
        ///// </summary>
        //public string BreakpointClass => IsExtraSmall ? "xs" :
        //    IsSmall ? "sm" :
        //    IsMedium ? "md" :
        //    IsLarge ? "lg" : 
        //    IsExtraLarge ? "xl" : "xxl";
    }
}