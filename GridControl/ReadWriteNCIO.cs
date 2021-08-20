using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GridControl
{
    public class ReadWriteNCIO
    {
        private List<Dimension> _dimList;
        private List<Variable> _variables = new List<Variable>();
        private Dimension _xDim = new Dimension(DimensionType.X);
        private Dimension _yDim = new Dimension(DimensionType.Y);
        private bool _xReverse = false;
        private bool _yReverse = false;
        private bool _isGlobal = false;
        private double _missingValue = -9999.0;
        public double[] levels;
        /// <summary>
        /// x coordinate array
        /// </summary>
        public double[] X;
        /// <summary>
        /// y coordinate array
        /// </summary>
        public double[] Y;
        public double[,] GRIDdata;
        
        /// <summary>
        /// x delt, y delt
        /// </summary>
        public double XDelt, YDelt;

        public Dimension XDimension
        {
            get { return _xDim; }
            set { _xDim = value; }
        }

        public double[,] GRIDData
        {
            get { return GRIDdata; }
            set { GRIDdata = value; }
        }

        /// <summary>
        /// Get or set Y dimension
        /// </summary>
        public Dimension YDimension
        {
            get { return _yDim; }
            set { _yDim = value; }
        }

        public double MissingValue
        {
            get { return _missingValue; }
            set { _missingValue = value; }
        }

        public bool IsXReverse
        {
            get { return _xReverse; }
            set { _xReverse = value; }
        }

        /// <summary>
        /// Get or set if y reversed
        /// </summary>
        public bool IsYReverse
        {
            get { return _yReverse; }
            set { _yReverse = value; }
        }

        /// <summary>
        /// Get or set if is global data
        /// </summary>
        public bool IsGlobal
        {
            get { return _isGlobal; }
            set { _isGlobal = value; }
        }

        public ReadWriteNCIO()
        {        
            _dimList = new List<Dimension>();
        }

        private bool GetVarData(int ncid, int varid, int dimLen, NetCDF.NcType ncType,
           ref double[] data)
        {
            int res, i;
            switch (ncType)
            {
                case NetCDF.NcType.NC_SHORT:
                    Int16[] varshort = new Int16[dimLen];
                    res = NetCDF.nc_get_var_short(ncid, varid, varshort);
                    if (res != 0) { goto ERROR; }
                    for (i = 0; i < dimLen; i++)
                    {
                        data[i] = (double)varshort[i];
                    }
                    break;
                case NetCDF.NcType.NC_INT:
                    int[] varint = new int[dimLen];
                    res = NetCDF.nc_get_var_int(ncid, varid, varint);
                    if (res != 0) { goto ERROR; }
                    for (i = 0; i < dimLen; i++)
                    {
                        data[i] = (double)varint[i];
                    }
                    break;
                case NetCDF.NcType.NC_FLOAT:
                    Single[] varfloat = new Single[dimLen];
                    res = NetCDF.nc_get_var_float(ncid, varid, varfloat);
                    if (res != 0) { goto ERROR; }
                    for (i = 0; i < dimLen; i++)
                    {
                        data[i] = (double)varfloat[i];
                    }
                    break;
                case NetCDF.NcType.NC_DOUBLE:
                    double[] vardouble = new double[dimLen];
                    res = NetCDF.nc_get_var_double(ncid, varid, vardouble);
                    if (res != 0) { goto ERROR; }
                    for (i = 0; i < dimLen; i++)
                    {
                        data[i] = (double)vardouble[i];
                    }
                    break;
            }

            return true;

            ERROR:
            return false;
        }

        private double GetValidVarAtt(AttStruct aAttS)
        {
            double aValue = 0;
            switch (aAttS.NCType)
            {
                case NetCDF.NcType.NC_INT:
                    aValue = ((int[])aAttS.attValue)[0];
                    break;
                case NetCDF.NcType.NC_SHORT:
                    aValue = ((Int16[])aAttS.attValue)[0];
                    break;
                case NetCDF.NcType.NC_BYTE:
                    aValue = ((byte[])aAttS.attValue)[0];
                    break;
                case NetCDF.NcType.NC_FLOAT:
                    aValue = ((Single[])aAttS.attValue)[0];
                    break;
                case NetCDF.NcType.NC_DOUBLE:
                    aValue = ((double[])aAttS.attValue)[0];
                    break;
            }

            return aValue;
        }

        /// <summary>
        /// Get NetCDF grid data - Lon/Lat
        /// </summary>
        /// <param name="timeIdx"></param>
        /// <param name="varIdx"></param>
        /// <param name="levelIdx"></param>        
        /// <returns></returns>
        public double[,] GetNetCDFGridData(int ncid, int timeIdx, int varIdx, int levelIdx)
        {
            int xNum, yNum;
            xNum = X.Length;
            yNum = Y.Length;
            double[,] gridData = new double[yNum, xNum];

            int res, i, j;
            Variable aVarS = new Variable();
            int dVarIdx = varIdx;
            int dVarNum = -1;
            for (i = 0; i < _variables.Count; i++)
            {
                //if (Variables[i].isDataVar)
                //    dVarNum += 1;
                if (dVarNum == varIdx)
                {
                    dVarIdx = i;
                    break;
                }
            }
            aVarS = _variables[dVarIdx];

            //Get pack info
            double add_offset, scale_factor;
            add_offset = 0;
            scale_factor = 1;
            for (i = 0; i < aVarS.Attributes.Count; i++)
            {
                if (aVarS.Attributes[i].attName == "add_offset")
                {
                    add_offset = GetValidVarAtt(aVarS.Attributes[i]);
                }

                if (aVarS.Attributes[i].attName == "scale_factor")
                {
                    scale_factor = GetValidVarAtt(aVarS.Attributes[i]);
                }

                if (aVarS.Attributes[i].attName == "missing_value")
                {
                    MissingValue = GetValidVarAtt(aVarS.Attributes[i]);
                }

                //MODIS NetCDF data
                if (aVarS.Attributes[i].attName == "_FillValue")
                {
                    MissingValue = GetValidVarAtt(aVarS.Attributes[i]);
                }
            }

            //Adjust undefine data
            MissingValue = MissingValue * scale_factor + add_offset;

            //Get grid data
            int varid;
            varid = aVarS.VarId;
            int[] start = new int[aVarS.DimNumber];
            int[] count = new int[aVarS.DimNumber];
            for (i = 0; i < aVarS.DimNumber; i++)
            {
                start[i] = 0;
                count[i] = 1;
            }
            if (aVarS.DimNumber == 4)
            {
                start[0] = timeIdx;
                start[1] = levelIdx;
                count[2] = yNum;
                count[3] = xNum;
            }
            else if (aVarS.DimNumber == 3)
            {
                start[0] = timeIdx;
                count[1] = yNum;
                count[2] = xNum;
            }
            else if (aVarS.DimNumber == 2)
            {
                count[0] = yNum;
                count[1] = xNum;
            }
            else
            {
                
            }
            double[] aData = new double[yNum * xNum];

            res = NetCDF.nc_get_vara_double(ncid, varid, start, count, aData);
            
            if (res != 0) return gridData;
            for (i = 0; i < yNum; i++)
            {
                for (j = 0; j < xNum; j++)
                {
                    gridData[i, j] = aData[i * xNum + j] * scale_factor + add_offset;
                    //gridData[i, j] = aData[i * xNum + j] + add_offset;
                }
            }

            if (this.IsGlobal)
            {
                double[,] newGridData;
                newGridData = new double[yNum, xNum + 1];
                double[] newX = new double[xNum + 1];
                for (i = 0; i < xNum; i++)
                    newX[i] = X[i];

                newX[xNum] = newX[xNum - 1] + XDelt;
                for (i = 0; i < yNum; i++)
                {
                    for (j = 0; j < xNum; j++)
                    {
                        newGridData[i, j] = gridData[i, j];
                    }
                    newGridData[i, xNum] = newGridData[i, 0];
                }

                gridData = newGridData;
            }

            if (this.IsYReverse)
            {
                double[,] nGridData = new double[gridData.GetLength(0), gridData.GetLength(1)];
                for (i = 0; i < gridData.GetLength(0); i++)
                {
                    for (j = 0; j < gridData.GetLength(1); j++)
                    {
                        nGridData[i, j] = gridData[gridData.GetLength(0) - i - 1, j];
                    }
                }
                gridData = nGridData;
            }

            return gridData;
        }

        private void GetDimValues(int ncid)
        {
            List<Variable> oneDimVars = new List<Variable>();
            foreach (Variable aVar in _variables)
            {
                if (aVar.DimNumber == 1)
                    oneDimVars.Add(aVar);
            }

            foreach (Dimension aDim in _dimList)
            {
                bool isFind = false;
                Variable aVar = null;
                foreach (Variable var in oneDimVars)
                {
                    if (aDim.DimName == var.Name)
                    {
                        isFind = true;
                        aVar = var;
                        break;
                    }
                }

                if (!isFind)
                {
                    foreach (Variable var in oneDimVars)
                    {
                        if (aDim.DimName == var.Dimensions[0].DimName)
                        {
                            isFind = true;
                            aVar = var;
                            break;
                        }
                    }
                }

                if (isFind)
                {
                    aVar.IsCoorVar = true;
                    double[] values = new double[aDim.DimLength];
                    GetVarData(ncid, aVar.VarId, aDim.DimLength, aVar.NCType, ref values);

                    //根据aDim 这个变量的名称，决定将数据绑定给谁
                    switch (aDim.DimName.ToString())
                    {
                        case "longitude":
                        case "projection_x_coordinate":
                        case "longitude_east":
                            X = values;
                            if (X[0] > X[1])
                            {
                                Array.Reverse(X);
                            }
                            XDelt = X[1] - X[0];
                            aDim.SetValues(X);
                            this.XDimension = aDim;
                            break;
                        case "latitude":
                        case "projection_y_coordinate":
                        case "latitude_north":
                            Y = values;
                            if (Y[0] > Y[1])
                            {
                                Array.Reverse(Y);
                            }
                            YDelt = Y[1] - Y[0];
                            aDim.SetValues(Y);
                            this.YDimension = aDim;
                            break;
                        case "time":
                        case "initial time":
                            //Get start time
                            int unitsId = 0;
                            string unitsStr;
                            int i;
                            for (i = 0; i < aVar.AttNumber; i++)
                            {
                                if (aVar.Attributes[i].attName == "units")
                                {
                                    unitsId = i;
                                    break;
                                }
                            }

                            unitsStr = (string)aVar.Attributes[unitsId].attValue;
                            break;
                    }
                }
            }
        }

        private bool ReadAtt(int ncid, int varid, int attnum, ref AttStruct aAttS)
        {
            int res;
            StringBuilder attName = new StringBuilder("", (int)NetCDF.NetCDF_limits.NC_MAX_NAME);
            res = NetCDF.nc_inq_attname(ncid, varid, attnum, attName);
            if (res != 0) { goto ERROR; }
            aAttS.attName = attName.ToString();
            int attLen, aType;
            res = NetCDF.nc_inq_att(ncid, varid, attName, out aType, out attLen);
            if (res != 0) { goto ERROR; }
            //res = NetCDF.nc_inq_atttype(ncid, varid, attName, out aType);
            //if (res != 0) { goto ERROR; }

            aAttS.NCType = (NetCDF.NcType)aType;
            aAttS.attLen = attLen;
            switch (aAttS.NCType)
            {
                case NetCDF.NcType.NC_CHAR:
                    StringBuilder atttext = new StringBuilder("", attLen);
                    res = NetCDF.nc_get_att_text(ncid, varid, attName.ToString(), atttext);
                    if (res != 0) { goto ERROR; }
                    aAttS.attValue = atttext.ToString();
                    if (((string)aAttS.attValue).Length > attLen)
                    {
                        aAttS.attValue = ((string)aAttS.attValue).Substring(0, attLen);
                    }
                    break;
                case NetCDF.NcType.NC_BYTE:
                    byte[] attbyte = new byte[attLen];
                    res = NetCDF.nc_get_att_uchar(ncid, varid, attName.ToString(), attbyte);
                    if (res != 0) { goto ERROR; }
                    aAttS.attValue = attbyte;
                    break;
                case NetCDF.NcType.NC_INT:
                    int[] attint = new int[attLen];
                    res = NetCDF.nc_get_att_int(ncid, varid, attName.ToString(), attint);
                    if (res != 0) { goto ERROR; }
                    aAttS.attValue = attint;
                    break;
                case NetCDF.NcType.NC_SHORT:
                    Int16[] attshort = new Int16[attLen];
                    res = NetCDF.nc_get_att_short(ncid, varid, attName.ToString(), attshort);
                    if (res != 0) { goto ERROR; }
                    aAttS.attValue = attshort;
                    break;
                case NetCDF.NcType.NC_FLOAT:
                    Single[] attfloat = new Single[attLen];
                    res = NetCDF.nc_get_att_float(ncid, varid, attName.ToString(), attfloat);
                    if (res != 0) { goto ERROR; }
                    aAttS.attValue = attfloat;
                    break;
                case NetCDF.NcType.NC_DOUBLE:
                    double[] attdouble = new double[attLen];
                    res = NetCDF.nc_get_att_double(ncid, varid, attName.ToString(), attdouble);
                    if (res != 0) { goto ERROR; }
                    aAttS.attValue = attdouble;
                    break;
            }

            return true;

            ERROR:
            return false;
        }

        private bool ReadVar(int ncid, int varid, ref Variable aVarS)
        {
            int res, i;
            int ndims, natts, aType;
            int[] dimids;
            StringBuilder varName = new StringBuilder("", (int)NetCDF.NetCDF_limits.NC_MAX_NAME);
            res = NetCDF.nc_inq_varndims(ncid, varid, out ndims);
            if (res != 0) { goto ERROR; }
            dimids = new int[ndims];
            res = NetCDF.nc_inq_var(ncid, varid, varName, out aType, out ndims, dimids, out natts);
            if (res != 0) { goto ERROR; }

            aVarS.Name = varName.ToString();
            aVarS.VarId = varid;
            //aVarS.nDims = ndims;
            aVarS.NCType = (NetCDF.NcType)aType;
            aVarS.AttNumber = natts;
            //aVarS.dimids = dimids;
            aVarS.Dimensions = new List<Dimension>();
            for (i = 0; i < dimids.Length; i++)
            {
                for (int j = 0; j < _dimList.Count; j++)
                {
                    if (_dimList[j].DimId == dimids[i])
                    {
                        aVarS.Dimensions.Add(_dimList[j]);
                        break;
                    }
                }
            }
            aVarS.Attributes = new List<AttStruct>();

            //Read variation attribute
            for (i = 0; i < natts; i++)
            {
                AttStruct aAtts = new AttStruct();
                ReadAtt(ncid, varid, i, ref aAtts);
                aVarS.Attributes.Add(aAtts);
            }
            
            return true;

            ERROR:
            return false;
        }

        public bool ReadNCFileSingleTime(String fileName)
        {
            int ncid, ndims, nvars, natts, unlimdimid;
            int res = NetCDF.nc_open(fileName, (int)NetCDF.CreateMode.NC_NOWRITE, out ncid);
            if (res != 0) return false;

            //取出数据，先读取几个dims  经度 维度 时间 观测值
            res = NetCDF.nc_inq(ncid, out ndims, out nvars, out natts, out unlimdimid);

            //Read dimensions
            StringBuilder dimName = new StringBuilder("", (int)NetCDF.NetCDF_limits.NC_MAX_NAME);
            int i, dimLen;
            List<string> dimNames = new List<string>();
            for (i = 0; i < ndims; i++)
            {
                dimName = new StringBuilder("", (int)NetCDF.NetCDF_limits.NC_MAX_NAME);
                res = NetCDF.nc_inq_dim(ncid, i, dimName, out dimLen);
                if (res != 0) { return false; }

                Dimension aDimS = new Dimension();
                aDimS.DimName = dimName.ToString();
                aDimS.DimLength = dimLen;
                aDimS.DimId = i;
                _dimList.Add(aDimS);
                dimNames.Add(aDimS.DimName);
            }

            //Read variables  
            List<Variable> varList = new List<Variable>();
            for (i = 0; i < nvars; i++)
            {
                Variable aVarS = new Variable();
                if (ReadVar(ncid, i, ref aVarS))
                {
                    varList.Add(aVarS);
                }
            }
            this._variables = varList;
            //解析数据
            GetDimValues(ncid);

            double[,] griddata = GetNetCDFGridData(ncid, 0, 2, 0);
            this.GRIDData = griddata;
            NetCDF.nc_close(ncid);
            return true;
        }
    }
}
