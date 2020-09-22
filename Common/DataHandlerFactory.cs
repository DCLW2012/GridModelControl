using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Common
{
    public class DataHandlerFactory
    {
        public static DataHandler CreaterHandlerByMethodName(string name)
        {
            DataHandler dataHandler = new DataHandler();
            if (name == "template")
            {
                dataHandler = new TemplateDataHandler();
            }
            return dataHandler;
        }
    }
}
