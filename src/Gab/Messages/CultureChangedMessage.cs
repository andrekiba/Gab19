using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Gab.Messages
{
    public class CultureChangedMessage
    {
        public CultureInfo NewCultureInfo { get; }

        public CultureChangedMessage(string lngName)
            : this(new CultureInfo(lngName))
        { }

        public CultureChangedMessage(CultureInfo newCultureInfo)
        {
            NewCultureInfo = newCultureInfo;
        }
    }
}
