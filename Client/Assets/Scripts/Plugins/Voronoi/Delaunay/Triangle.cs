﻿using System.Collections;
using System.Collections.Generic;

namespace Plugins.Voronoi
{
    public class Triangle
    {
        private List<Site> sites;

        public List<Site> Sites
        {
            get { return sites; }
        }

        public Triangle(Site a, Site b, Site c)
        {
            sites = new List<Site>();
            sites.Add(a);
            sites.Add(b);
            sites.Add(c);
        }

        public void Dispose()
        {
            sites.Clear();
        }
    }
}