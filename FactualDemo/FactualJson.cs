namespace FactualAPIProject1
{
    class FactualJson
    {
        public int version { get; set; }
        public FactualData response { get; set; }
    }

    class FactualData
    {
        public FactualPoint[] data { get; set; }
        public int included_rows { get; set; }
    }
    public class FactualPoint
    {
        public string region { get; set; }
        public string name { get; set; }
        public string longitude { get; set; }
        public string website { get; set; }
        public string postcode { get; set; }
        public string country { get; set; }
        public string address { get; set; }
        public string locality { get; set; }
        public string latitude { get; set; }
        public string factual_id { get; set; }
        public string distance { get; set; }
    }
}
