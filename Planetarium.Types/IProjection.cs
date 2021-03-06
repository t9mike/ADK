﻿using ADK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planetarium.Types
{
    public interface IProjection
    {
        PointF Project(CrdsHorizontal hor);
        CrdsHorizontal Invert(PointF point);
    }
}
