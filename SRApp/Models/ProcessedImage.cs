namespace SRApp.Models
{
    public class ProcessedImage
    {
        public int ImageId { get; set; }
        public int UserId { get; set; }
        public required string OriginalImagePath { get; set; }
        public required string ProcessedImagePath { get; set; }
        public DateTime ProcessedAt { get; set; }
        public required string Status { get; set; }
        public required string Metadata { get; set; }
        public required string ScaleOption { get; set; }

        // Ruta calculada para la imagen original
        public string FullOriginalImageUrl
        {
            get
            {
                return "http://35.222.132.235:5164" + OriginalImagePath;
            }
        }

        // Ruta calculada para la imagen procesada
        public string FullProcessedImageUrl
        {
            get
            {
                return "http://35.222.132.235:5164" + ProcessedImagePath;
            }
        }
    }
}
