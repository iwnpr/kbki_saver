using db_lib.Entities;

namespace db_lib
{
    public static class DataHelper
    {
        public static List<TrAbonent>? Abonents { get; set; }
        public static List<TrAbonentCertificate>? AbonentCertificates { get; set; }
        public static List<TdUser>? Users { get; set; }
    }
}
