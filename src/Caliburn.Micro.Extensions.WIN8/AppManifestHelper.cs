﻿﻿//
// Copyright (c) 2012 Tim Heuer
//
// Licensed under the Microsoft Public License (Ms-PL) (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://opensource.org/licenses/Ms-PL.html
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Caliburn.Micro
{
    internal class AppManifestHelper
    {
        private const string DisplayNameAttributeName = "DisplayName";
        private const string DescriptionAttributeName = "Description";
        private const string LogoAttributeName = "Square150x150Logo";
        private const string BackgroundColorAttributeName = "BackgroundColor";
#if WINDOWS_UAP
        private const string SmallLogoAttributeName = "Square44x44Logo";
        private const string VisualElementsNamespaceName = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
#else
        private const string SmallLogoAttributeName = "Square30x30Logo";
        private const string VisualElementsNamespaceName = "http://schemas.microsoft.com/appx/2013/manifest";
#endif

        public async static Task<VisualElement> GetManifestVisualElementsAsync()
        {
            // the path for the manifest
            XDocument xmldoc;
            using (Stream manifestStream = await Windows.ApplicationModel.Package.Current.InstalledLocation.OpenStreamForReadAsync("AppxManifest.xml"))
            {
                xmldoc = XDocument.Load(manifestStream);
            }

            // set the XNamespace and name for the VisualElements node we want
            var xn = XName.Get("VisualElements", VisualElementsNamespaceName);

            // parse the VisualElements node only, pulling out what we need
            // NOTE: This will get only the first Application (which is the mainstream case)
            // TODO: Need to take into account that DisplayName/Description may be localized using ms-resource:{foo}
            var visualElementNode = (from vel in xmldoc.Descendants(xn)
                                     select new VisualElement
                                     {
                                         DisplayName = vel.Attribute(DisplayNameAttributeName).Value,
                                         Description = vel.Attribute(DescriptionAttributeName).Value,
                                         LogoUri = new Uri(string.Format("ms-appx:///{0}", vel.Attribute(LogoAttributeName).Value.Replace(@"\", @"/"))),
                                         SmallLogoUri = new Uri(string.Format("ms-appx:///{0}", vel.Attribute(SmallLogoAttributeName).Value.Replace(@"\", @"/"))),
                                         BackgroundColorAsString = vel.Attribute(BackgroundColorAttributeName).Value
                                     }).FirstOrDefault();
            
            if (visualElementNode == null) 
                throw new ArgumentNullException("Could not parse the VisualElements from the app manifest.");

            return visualElementNode;
        }

    }

    internal class VisualElement
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public Uri LogoUri { get; set; }
        public Uri SmallLogoUri { get; set; }
        public string BackgroundColorAsString { get; set; }
        public Windows.UI.Color BackgroundColor
        {
            get
            {
                return ToColor(BackgroundColorAsString);
            }
        }

        private static Windows.UI.Color ToColor(string hexValue)
        {
            hexValue = hexValue.Replace("#", string.Empty);

            // some loose validation (not bullet-proof)
            if (hexValue.Length < 6)
            {
                throw new ArgumentException("This does not appear to be a proper hex color number");
            }

            byte a = 255;
            byte r = 255;
            byte g = 255;
            byte b = 255;

            int startPosition = 0;

            // the case where alpha is provided
            if (hexValue.Length == 8)
            {
                a = byte.Parse(hexValue.Substring(0, 2), NumberStyles.HexNumber);
                startPosition = 2;
            }

            r = byte.Parse(hexValue.Substring(startPosition, 2), NumberStyles.HexNumber);
            g = byte.Parse(hexValue.Substring(startPosition + 2, 2), NumberStyles.HexNumber);
            b = byte.Parse(hexValue.Substring(startPosition + 4, 2), NumberStyles.HexNumber);

            return Windows.UI.Color.FromArgb(a, r, g, b);
        }
    }
}