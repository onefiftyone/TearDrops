using OneFiftyOne.TearDrops.Common.Configuration;
using OneFiftyOne.TearDrops.Common.Logging;
using OneFiftyOne.TearDrops.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            BaseRepository.ChangeConnection(null, "Test", "Data Source=ATLAS;Initial Catalog=Empty;Integrated Security=yes;", "System.Data.SqlClient");
            var test = BaseRepository.ActiveConnection;
        }
    }
}               
