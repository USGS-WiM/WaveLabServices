//------------------------------------------------------------------------------
//----- NavigationHypermedia ---------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2017 WIM - USGS

//    authors:  Jeremy K. Newson USGS Web Informatics and Mapping
//              
//  
//   purpose:   Intersects the pipeline after
//
//discussion:   Controllers are objects which handle all interaction with resources. 
//              
//
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WIM.Hypermedia;
using WIM.Resources;
using WIM.Services.Filters;

namespace WaveLabServices.Filters
{
    public class WaveLabHypermedia : HypermediaBase
    {
        protected override List<Link> GetEnumeratedHypermedia(IHypermedia entity)
        {
            List<Link> results = null;
            switch (entity.GetType().Name)
            {
                case "WaveLab":
                    results = new List<Link>();
                    results.Add(Hyperlinks.Generate(BaseURI, "self by id", this.URLQuery +"/", WIM.Resources.refType.GET));
                    break;

                default:
                    break;
            }

            return results;

        }

        protected override List<Link> GetReflectedHypermedia(IHypermedia entity)
        {
            List<Link> results = null;
            switch (entity.GetType().Name)
            {
                case "WaveLab":
                    results = new List<Link>();
                    results.Add(Hyperlinks.Generate(BaseURI, "wavelab example", this.URLQuery + "/", WIM.Resources.refType.POST));

                    break;                
                default:
                    break;
            }

            return results;
        }
    }
}
