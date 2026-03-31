using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace db_lib.Models.DTO
{
    public class PackageError
    {
        public int Id { get; set; }

        public int error_code { get; set; }

        public string? error_message { get; set; }
    }

}
