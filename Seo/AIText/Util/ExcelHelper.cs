using commons.util;
using Microsoft.AspNetCore.Http;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace commons.util
{
    public class ExcelHelper<T> where T : new()
    {

        #region List导出到Excel文件
        /// <summary>
        /// List导出到Excel文件
        /// </summary>
        /// <param name="sFileName"></param>
        /// <param name="sHeaderText"></param>
        /// <param name="list"></param>
        public static string ExportToExcel(string rootpath, string sFileName, string sHeaderText, List<T> list, string[] columns)
        {
            sFileName = string.Format("{0}_{1}", Guid.NewGuid().ToString().Replace("-", string.Empty).ToLower(), sFileName);
            string sRoot = rootpath;
            string partDirectory = string.Format("Resource{0}Export{0}Excel", Path.DirectorySeparatorChar);
            string sDirectory = Path.Combine(sRoot, partDirectory);
            string sFilePath = Path.Combine(sDirectory, sFileName);
            if (!Directory.Exists(sDirectory))
            {
                Directory.CreateDirectory(sDirectory);
            }
            using (MemoryStream ms = CreateExportMemoryStream(list, sHeaderText, columns))
            {
                using (FileStream fs = new FileStream(sFilePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] data = ms.ToArray();
                    fs.Write(data, 0, data.Length);
                    fs.Flush();
                }
            }
            return partDirectory + Path.DirectorySeparatorChar + sFileName;
        }

        /// <summary>
        /// List导出到Excel文件
        /// </summary>
        /// <param name="sFileName"></param>
        /// <param name="sHeaderText"></param>
        /// <param name="list"></param>
        /// <returns>.xls文件流</returns>
        public static byte[] ExportToExcel(string sHeaderText, List<T> list, string[] columns)
        {
            byte[] bytes = new byte[0];
            using (MemoryStream ms = CreateExportMemoryStream(list, sHeaderText, columns))
            {
                bytes = ms.ToArray();
                ms.Close();
            }
            return bytes;
        }

        /// <summary>
        /// List导出到Excel文件
        /// </summary>
        /// <param name="sFileName"></param>
        /// <param name="list"></param>
        /// <param name="customs">自定义表头</param>
        /// <returns>.xls文件流</returns>
        public static byte[] ExportToExcel(List<T> list, string[] columns, IList<string[]> customs = null)
        {
            byte[] bytes = new byte[0];
            using (MemoryStream ms = CreateExportMemoryStream(list, columns, customs))
            {
                bytes = ms.ToArray();
                ms.Close();
            }
            return bytes;
        }

        #endregion

        #region Excel导入
        /// <summary>
        /// Excel导入
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<T> ImportFromExcel(string rootpath, string filePath)
        {
            string absoluteFilePath = rootpath + filePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            List<T> list = new List<T>();
            HSSFWorkbook hssfWorkbook = null;
            XSSFWorkbook xssWorkbook = null;
            ISheet sheet = null;

            using (FileStream file = new FileStream(absoluteFilePath, FileMode.Open, FileAccess.Read))
            {
                switch (Path.GetExtension(filePath))
                {
                    case ".xls":
                        hssfWorkbook = new HSSFWorkbook(file);
                        sheet = hssfWorkbook.GetSheetAt(0);
                        break;

                    case ".xlsx":
                        xssWorkbook = new XSSFWorkbook(file);
                        sheet = xssWorkbook.GetSheetAt(0);
                        break;

                    default:
                        throw new Exception("不支持的文件格式");
                }
            }
            return CreateExel(list, sheet, hssfWorkbook, xssWorkbook);
        }

        /// <summary>
        ///  内存Excel导入
        /// </summary>
        /// <param name="importFile"></param>
        /// <returns></returns>
        public static async Task<List<T>> ImportFromExcel(IFormFile importFile)
        {
            List<T> list = new List<T>();
            HSSFWorkbook hssfWorkbook = null;
            XSSFWorkbook xssWorkbook = null;
            ISheet sheet = null;

            using (var file = new MemoryStream())
            {
                await importFile.CopyToAsync(file);//取到文件流
                file.Seek(0, SeekOrigin.Begin);
                switch (Path.GetExtension(importFile.FileName))
                {
                    case ".xls":
                        hssfWorkbook = new HSSFWorkbook(file);
                        sheet = hssfWorkbook.GetSheetAt(0);
                        break;

                    case ".xlsx":
                        xssWorkbook = new XSSFWorkbook(file);
                        sheet = xssWorkbook.GetSheetAt(0);
                        break;

                    default:
                        throw new Exception("不支持的文件格式");
                }
            }

            return CreateExel(list, sheet, hssfWorkbook, xssWorkbook);
        }

        #endregion

        #region Excel导出

        static IWorkbook workbook;

        /// <summary>
        /// 说    明：创建导出文件
        /// </summary>
        /// <param name="tempName">模板名称</param>
        /// <param name="rowNum">数据起始行位置</param>
        /// <param name="keyList">关键字</param>
        /// <param name="dateList">导出数据</param>
        public byte[] GetExport(string tempName, int rowNum, List<T> dateList)
        {
            return DoExport("sheet1", tempName, rowNum, dateList);
        }

        /// <summary>
        /// 说    明：创建导出文件
        /// </summary>
        /// <param name="name">工作表名称</param>
        /// <param name="tempName">模板名称</param>
        /// <param name="rowNum">数据起始行位置</param>
        /// <param name="keyList">关键字</param>
        /// <param name="dateList">导出数据</param>
        private byte[] DoExport(string name, string tempName, int rowNum, List<T> dateList)
        {
            InitializeWorkbook(tempName);

            NPOI.SS.UserModel.ISheet sheet = workbook.GetSheet(name);
            int startRowIndex = rowNum;//数据起始行位置
            Type type = typeof(T);
            PropertyInfo[] pis = type.GetProperties();
            int pisLen = pis.Length;
            PropertyInfo pi = null;

            var conNextRow = true;
            int n = startRowIndex;
            foreach (var data in dateList)
            {
                if (n == dateList.Count + startRowIndex - 1)
                {
                    conNextRow = false;
                }

                int piIndex = 0;
                while (piIndex < pisLen)
                {
                    pi = pis[piIndex];
                    var val = pi.GetValue(data, null) == null ? "" : pi.GetValue(data, null);
                    SetCellValueByTemplateStr(sheet, n, "{$" + pi.Name + "}", val, conNextRow);
                    piIndex++;
                }
                n++;
            }


            sheet.ForceFormulaRecalculation = true;

            var bytes = WriteToFile();
            return bytes;
        }

        /// <summary>
        /// 根据Excel模板单元格内容，找出单元格，并设置单元格的值
        /// </summary>
        /// <param name="sheet">ExcelSheet</param>
        /// <param name="rowIndex">行索引</param>
        /// <param name="cellTemplateValue">模板内容</param>
        /// <param name="cellFillValue">单元格值</param>
        ///  <param name="conNextRow">是否承接下一行，即：填充下一行单元格模板内容</param>
        public static void SetCellValueByTemplateStr(NPOI.SS.UserModel.ISheet sheet, int rowIndex, string cellTemplateValue, object cellFillValue, bool conNextRow = true)
        {
            int cellStartIndex = sheet.GetRow(rowIndex).FirstCellNum;
            int cellEndIndex = sheet.GetRow(rowIndex).LastCellNum;
            bool find = false;
            for (int i = cellStartIndex; i < cellEndIndex; i++)
            {
                if (find)
                    break;
                else
                    if (i < (cellEndIndex - cellStartIndex) / 2)
                {
                    #region 折半查找：前半段
                    for (int j = i; j < (cellEndIndex - cellStartIndex) / 2; j++)
                    {
                        if (find)
                            break;
                        else
                        {
                            var cell = sheet.GetRow(rowIndex).GetCell(j);
                            if (cell != null)
                            {
                                if (cell.CellType == CellType.String)
                                {
                                    string strCellValue = sheet.GetRow(rowIndex).GetCell(j).StringCellValue;
                                    if (string.Compare(strCellValue, cellTemplateValue, true) == 0)
                                    {
                                        find = true;
                                        Type type = cellFillValue.GetType();
                                        switch (type.ToString())
                                        {
                                            case "System.String":
                                                string strValue = cellFillValue.ToString();
                                                sheet.GetRow(rowIndex).GetCell(j).SetCellValue(strValue);
                                                break;
                                            case "System.Int32":
                                                int intValue = Convert.ToInt32(cellFillValue.ToString());
                                                sheet.GetRow(rowIndex).GetCell(j).SetCellValue(intValue);
                                                break;
                                            case "System.Decimal":
                                                double decimalValue = Convert.ToDouble(cellFillValue.ToString());
                                                sheet.GetRow(rowIndex).GetCell(j).SetCellValue(decimalValue);
                                                break;
                                            case "System.DateTime":
                                                DateTime timeValue = Convert.ToDateTime(cellFillValue.ToString());
                                                if (timeValue == DateTime.MinValue)
                                                {
                                                    sheet.GetRow(rowIndex).GetCell(j).SetCellValue("");
                                                }
                                                else
                                                {
                                                    sheet.GetRow(rowIndex).GetCell(j).SetCellValue(timeValue.ToString("yyyy/MM/dd"));
                                                }
                                                break;

                                        }
                                        if (conNextRow)//如果承接下一行，则设置下一行单元格里的模板内容
                                        {
                                            //如果为空，则创建
                                            if (sheet.GetRow(rowIndex + 1) == null)
                                            {
                                                sheet.CreateRow(rowIndex + 1);
                                            }
                                            if (sheet.GetRow(rowIndex + 1).GetCell(j) == null)
                                            {
                                                sheet.GetRow(rowIndex + 1).CreateCell(j);
                                            }
                                            sheet.GetRow(rowIndex + 1).GetCell(j).SetCellValue(cellTemplateValue);
                                            sheet.GetRow(rowIndex + 1).GetCell(j).SetCellType(CellType.String);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region 折半查找：后半段
                    for (int j = (cellEndIndex - cellStartIndex) / 2; j < cellEndIndex; j++)
                    {
                        if (find)
                            break;
                        else
                        {
                            var cell = sheet.GetRow(rowIndex).GetCell(j);
                            if (cell != null)
                            {
                                if (cell.CellType == CellType.String)
                                {
                                    string strCellValue = sheet.GetRow(rowIndex).GetCell(j).StringCellValue;
                                    if (string.Compare(strCellValue, cellTemplateValue, true) == 0)
                                    {
                                        find = true;
                                        Type type = cellFillValue.GetType();
                                        switch (type.ToString())
                                        {
                                            case "System.String":
                                                string strValue = cellFillValue.ToString();
                                                sheet.GetRow(rowIndex).GetCell(j).SetCellValue(strValue);
                                                break;
                                            case "System.Int32":
                                                int intValue = Convert.ToInt32(cellFillValue.ToString());
                                                sheet.GetRow(rowIndex).GetCell(j).SetCellValue(intValue);
                                                break;
                                            case "System.Decimal":
                                                double decimalValue = Convert.ToDouble(cellFillValue.ToString());
                                                sheet.GetRow(rowIndex).GetCell(j).SetCellValue(decimalValue);
                                                break;
                                            case "System.DateTime":
                                                DateTime timeValue = Convert.ToDateTime(cellFillValue.ToString());
                                                if (timeValue == DateTime.MinValue)
                                                {
                                                    sheet.GetRow(rowIndex).GetCell(j).SetCellValue("");
                                                }
                                                else
                                                {
                                                    sheet.GetRow(rowIndex).GetCell(j).SetCellValue(timeValue.ToString("yyyy/MM/dd"));
                                                }
                                                break;
                                        }
                                        if (conNextRow)//如果承接下一行，则设置下一行单元格里的模板内容
                                        {
                                            //如果为空，则创建
                                            if (sheet.GetRow(rowIndex + 1) == null)
                                            {
                                                sheet.CreateRow(rowIndex + 1);
                                            }
                                            if (sheet.GetRow(rowIndex + 1).GetCell(j) == null)
                                            {
                                                sheet.GetRow(rowIndex + 1).CreateCell(j);
                                            }
                                            sheet.GetRow(rowIndex + 1).GetCell(j).SetCellValue(cellTemplateValue);
                                            sheet.GetRow(rowIndex + 1).GetCell(j).SetCellType(CellType.String);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// 创建写入
        /// </summary>
        /// <returns></returns>
        private byte[] WriteToFile()
        {
            //使用内存流
            byte[] bytes = new byte[0];
            using (MemoryStream ms = new MemoryStream())
            {
                workbook.Write(ms);
                bytes = ms.ToArray();
                ms.Close();
            }
            return bytes;
        }

        /// <summary>
        /// 打开模板
        /// </summary>
        /// <param name="tempName"></param>
        private void InitializeWorkbook(string tempName)
        {
            string savePath = System.IO.Directory.GetCurrentDirectory() + "/wwwroot/template/" + tempName + ".xlsx";
            using (FileStream sw = new FileStream(savePath, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(sw);
            }
        }


        #endregion Excel导出

        #region Private Method

        private static ConcurrentDictionary<string, object> dictCache = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// 判断一行是否为空（所有单元格都为空或仅包含空白字符）
        /// </summary>
        static bool IsRowEmpty(IRow row)
        {
            if (row == null) return true; // 行对象本身为空

            foreach (ICell cell in row.Cells) // 遍历所有单元格
            {
                if (cell.CellType != CellType.Blank) // 如果单元格不是空白
                {
                    if (cell.CellType == CellType.String && !string.IsNullOrWhiteSpace(cell.StringCellValue))
                    {
                        return false; // 发现非空字符串单元格
                    }
                    else if (cell.CellType != CellType.String)
                    {
                        return false; // 发现非空的非字符串单元格（数字、日期等）
                    }
                }
            }
            return true; // 所有单元格都是空的
        }

        /// <summary>
        /// 将excel文件转换成list
        /// </summary>
        /// <param name="list"></param>
        /// <param name="sheet"></param>
        /// <param name="hssfWorkbook"></param>
        /// <param name="xssWorkbook"></param>
        /// <returns></returns>
        private static List<T> CreateExel(List<T> list, ISheet sheet = null, HSSFWorkbook hssfWorkbook = null, XSSFWorkbook xssWorkbook = null)
        {
            IRow columnRow = sheet.GetRow(0); // 第一行为字段名
            Dictionary<int, PropertyInfo> mapPropertyInfoDict = new Dictionary<int, PropertyInfo>();
            for (int j = 0; j < columnRow.LastCellNum; j++)
            {
                ICell cell = columnRow.GetCell(j);
                PropertyInfo propertyInfo = MapPropertyInfo(cell.ParseToString().TrimEnd('*'));
                if (propertyInfo != null)
                {
                    mapPropertyInfoDict.Add(j, propertyInfo);
                }
            }

            for (int i = (sheet.FirstRowNum + 1); i < sheet.LastRowNum + 1; i++)
            {
                IRow row = sheet.GetRow(i);
                if (IsRowEmpty(row))
                {
                    continue;
                }
                T entity = new T();
                for (int j = row.FirstCellNum; j <= columnRow.LastCellNum; j++)
                {
                    if (mapPropertyInfoDict.ContainsKey(j))
                    {
                        if (row.GetCell(j) != null)
                        {
                            PropertyInfo propertyInfo = mapPropertyInfoDict[j];
                            switch (propertyInfo.PropertyType.ToString())
                            {
                                case "System.DateTime":
                                case "System.Nullable`1[System.DateTime]":
                                    var dateCell = row.GetCell(j);
                                    DateTime date;
                                    // 单元格为日期格式（数值）时直接取值，不需要转换
                                    if (dateCell.CellType == CellType.Numeric)
                                        date = dateCell.DateCellValue;
                                    else
                                        // 否则尝试进行转换
                                        date = dateCell.ParseToString().ParseToDateTime();

                                    mapPropertyInfoDict[j].SetValue(entity, date);
                                    break;

                                case "System.Boolean":
                                case "System.Nullable`1[System.Boolean]":
                                    mapPropertyInfoDict[j].SetValue(entity, row.GetCell(j).ParseToString().ParseToBool());
                                    break;

                                case "System.Byte":
                                case "System.Nullable`1[System.Byte]":
                                    mapPropertyInfoDict[j].SetValue(entity, Byte.Parse(row.GetCell(j).ParseToString()));
                                    break;
                                case "System.Int16":
                                case "System.Nullable`1[System.Int16]":
                                    mapPropertyInfoDict[j].SetValue(entity, Int16.Parse(row.GetCell(j).ParseToString()));
                                    break;
                                case "System.Int32":
                                case "System.Nullable`1[System.Int32]":
                                    mapPropertyInfoDict[j].SetValue(entity, row.GetCell(j).ParseToString().ParseToInt());
                                    break;

                                case "System.Int64":
                                case "System.Nullable`1[System.Int64]":
                                    mapPropertyInfoDict[j].SetValue(entity, row.GetCell(j).ParseToString().ParseToLong());
                                    break;

                                case "System.Double":
                                case "System.Nullable`1[System.Double]":
                                    mapPropertyInfoDict[j].SetValue(entity, row.GetCell(j).ParseToString().ParseToDouble());
                                    break;

                                case "System.Decimal":
                                case "System.Nullable`1[System.Decimal]":
                                    mapPropertyInfoDict[j].SetValue(entity, row.GetCell(j).ParseToString().ParseToDecimal());
                                    break;

                                default:
                                case "System.String":
                                    mapPropertyInfoDict[j].SetValue(entity, row.GetCell(j).ParseToString());
                                    break;
                            }
                        }
                    }
                }
                list.Add(entity);
            }
            hssfWorkbook?.Close();
            xssWorkbook?.Close();
            return list;
        }

        /// <summary>  
        /// List导出到Excel的MemoryStream   
        /// </summary>  
        /// <param name="list">数据源</param>  
        /// <param name="sHeaderText">表头文本</param>  
        /// <param name="columns">需要导出的属性</param>  
        private static MemoryStream CreateExportMemoryStream(List<T> list, string sHeaderText, string[] columns)
        {
            HSSFWorkbook workbook = new HSSFWorkbook();
            ISheet sheet = workbook.CreateSheet();

            Type type = typeof(T);
            PropertyInfo[] properties = GetProperties(type, columns);

            ICellStyle dateStyle = workbook.CreateCellStyle();
            IDataFormat format = workbook.CreateDataFormat();
            dateStyle.DataFormat = format.GetFormat("yyyy-MM-dd");

            #region 取得每列的列宽（最大宽度）
            int[] arrColWidth = new int[properties.Length];
            for (int columnIndex = 0; columnIndex < properties.Length; columnIndex++)
            {
                //GBK对应的code page是CP936
                arrColWidth[columnIndex] = properties[columnIndex].Name.Length;
            }
            #endregion
            for (int rowIndex = 0; rowIndex < list.Count; rowIndex++)
            {
                #region 新建表，填充表头，填充列头，样式
                if (rowIndex == 65535 || rowIndex == 0)
                {
                    if (rowIndex != 0)
                    {
                        sheet = workbook.CreateSheet();
                    }

//                    #region 表头及样式
//                    {
//                        IRow headerRow = sheet.CreateRow(0);
//                        headerRow.HeightInPoints = 25;
//                        headerRow.CreateCell(0).SetCellValue(sHeaderText);

//                        ICellStyle headStyle = workbook.CreateCellStyle();
//                        headStyle.Alignment = HorizontalAlignment.Center;
//                        IFont font = workbook.CreateFont();
//                        font.FontHeightInPoints = 20;
//#pragma warning disable CS0618 // '“IFont.Boldweight”已过时:“deprecated POI 3.15 beta 2. Use IsBold instead.”
//                        font.Boldweight = 700;
//#pragma warning restore CS0618 // '“IFont.Boldweight”已过时:“deprecated POI 3.15 beta 2. Use IsBold instead.”
//                        headStyle.SetFont(font);

//                        headerRow.GetCell(0).CellStyle = headStyle;

//                        sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, properties.Length - 1));
//                    }
//                    #endregion

                    #region 列头及样式
                    {
                        IRow headerRow = sheet.CreateRow(0);
                     //   ICellStyle headStyle = workbook.CreateCellStyle();
                     //   headStyle.Alignment = HorizontalAlignment.Center;
                        IFont font = workbook.CreateFont();
                        font.FontHeightInPoints = 10;
#pragma warning disable CS0618 // '“IFont.Boldweight”已过时:“deprecated POI 3.15 beta 2. Use IsBold instead.”
                        font.Boldweight = 700;
#pragma warning restore CS0618 // '“IFont.Boldweight”已过时:“deprecated POI 3.15 beta 2. Use IsBold instead.”
                      //  headStyle.SetFont(font);

                        for (int columnIndex = 0; columnIndex < properties.Length; columnIndex++)
                        {
                            // 类属性如果有Description就用Description当做列名
                            DescriptionAttribute customAttribute = (DescriptionAttribute)Attribute.GetCustomAttribute(properties[columnIndex], typeof(DescriptionAttribute));
                            string description = properties[columnIndex].Name;
                            if (customAttribute != null)
                            {
                                description = customAttribute.Description;
                            }
                            headerRow.CreateCell(columnIndex).SetCellValue(description);
                          //  headerRow.GetCell(columnIndex).CellStyle = headStyle;

                            //设置列宽  
                            sheet.SetColumnWidth(columnIndex, (arrColWidth[columnIndex] + 1) * 256);
                        }
                    }
                    #endregion
                }
                #endregion

                #region 填充内容
              //  ICellStyle contentStyle = workbook.CreateCellStyle();
              //  contentStyle.Alignment = HorizontalAlignment.Left;
                IRow dataRow = sheet.CreateRow(rowIndex + 1); // 前面2行已被占用
                for (int columnIndex = 0; columnIndex < properties.Length; columnIndex++)
                {
                    ICell newCell = dataRow.CreateCell(columnIndex);
                 //   newCell.CellStyle = contentStyle;

                    string drValue = properties[columnIndex].GetValue(list[rowIndex], null).ParseToString();
                    switch (properties[columnIndex].PropertyType.ToString())
                    {
                        case "System.String":
                            newCell.SetCellValue(drValue);
                            break;

                        case "System.DateTime":
                        case "System.Nullable`1[System.DateTime]":
                            newCell.SetCellValue(drValue.ParseToDateTime());
                            newCell.CellStyle = dateStyle; //格式化显示  
                            break;

                        case "System.Boolean":
                        case "System.Nullable`1[System.Boolean]":
                            newCell.SetCellValue(drValue.ParseToBool());
                            break;

                        case "System.Byte":
                        case "System.Nullable`1[System.Byte]":
                        case "System.Int16":
                        case "System.Nullable`1[System.Int16]":
                        case "System.Int32":
                        case "System.Nullable`1[System.Int32]":
                            newCell.SetCellValue(drValue.ParseToInt());
                            break;

                        case "System.Int64":
                        case "System.Nullable`1[System.Int64]":
                            newCell.SetCellValue(drValue.ParseToString());
                            break;

                        case "System.Double":
                        case "System.Nullable`1[System.Double]":
                            newCell.SetCellValue(drValue.ParseToDouble());
                            break;

                        case "System.Decimal":
                        case "System.Nullable`1[System.Decimal]":
                            newCell.SetCellValue(drValue);
                            break;

                        case "System.DBNull":
                            newCell.SetCellValue(string.Empty);
                            break;

                        default:
                            newCell.SetCellValue(string.Empty);
                            break;
                    }
                }
                #endregion
            }

            //列宽自适应，只对英文和数字有效
            //for (int i = 0; i <= properties.Length; i++)
            //{
            //    sheet.AutoSizeColumn(i);
            //}
            //列宽自适应
            for (int columnNum = 0; columnNum <= properties.Length; columnNum++)
            {
                int columnWidth = sheet.GetColumnWidth(columnNum) / 256;
                for (int rowNum = 1; rowNum <= sheet.LastRowNum; rowNum++)
                {
                    IRow currentRow;
                    //当前行未被使用过
                    if (sheet.GetRow(rowNum) == null)
                    {
                        currentRow = sheet.CreateRow(rowNum);
                    }
                    else
                    {
                        currentRow = sheet.GetRow(rowNum);
                    }

                    if (currentRow.GetCell(columnNum) != null)
                    {
                        ICell currentCell = currentRow.GetCell(columnNum);
                        int length = Encoding.Default.GetBytes(currentCell.ToString()).Length;
                        if (columnWidth < length)
                        {
                            columnWidth = length;
                        }
                    }
                }
                if (columnWidth > 255) columnWidth = 255;
                sheet.SetColumnWidth(columnNum, columnWidth * 256);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                workbook.Write(ms);
                workbook.Close();
                ms.Flush();
                ms.Position = 0;
                return ms;
            }
        }

        /// <summary>  
        /// List导出到Excel的XSSFWorkbook  
        /// </summary>  
        /// <param name="list">数据源</param>  
        /// <param name="columns">需要导出的属性</param>  
        /// <param name="customs">自定义表头</param>
        public static XSSFWorkbook CreateExportXss(List<T> list, string[] columns, IList<string[]> customs = null)
        {
            XSSFWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet();
            
            
            Type type = typeof(T);
            PropertyInfo[] properties = GetProperties(type, columns);

            ICellStyle dateStyle = workbook.CreateCellStyle();
            IDataFormat format = workbook.CreateDataFormat();
            dateStyle.DataFormat = format.GetFormat("yyyy-MM-dd");

            #region 取得每列的列宽（最大宽度）
            int[] arrColWidth = new int[properties.Length];
            for (int columnIndex = 0; columnIndex < properties.Length; columnIndex++)
            {
                //GBK对应的code page是CP936
                arrColWidth[columnIndex] = properties[columnIndex].Name.Length;
            }
            #endregion

            int startRow = 1;
            for (int rowIndex = 0; rowIndex < list.Count; rowIndex++)
            {
                #region 新建表，填充表头，填充列头，样式
                if (rowIndex == 65535 || rowIndex == 0)
                {
                    if (rowIndex != 0)
                    {
                        sheet = workbook.CreateSheet();
                    }

                    #region 列头及样式
                    {
                        IRow headerRow = sheet.CreateRow(0);
                        ICellStyle headStyle = workbook.CreateCellStyle();
                        headStyle.Alignment = HorizontalAlignment.Center;
                        IFont font = workbook.CreateFont();
                        font.FontHeightInPoints = 10;
#pragma warning disable CS0618 // '“IFont.Boldweight”已过时:“deprecated POI 3.15 beta 2. Use IsBold instead.”
                        font.Boldweight = 700;
#pragma warning restore CS0618 // '“IFont.Boldweight”已过时:“deprecated POI 3.15 beta 2. Use IsBold instead.”
                        headStyle.SetFont(font);

                        for (int columnIndex = 0; columnIndex < properties.Length; columnIndex++)
                        {
                            // 类属性如果有Description就用Description当做列名
                            DescriptionAttribute customAttribute = (DescriptionAttribute)Attribute.GetCustomAttribute(properties[columnIndex], typeof(DescriptionAttribute));
                            string description = properties[columnIndex].Name;
                            if (customAttribute != null)
                            {
                                description = customAttribute.Description;
                            }
                            headerRow.CreateCell(columnIndex).SetCellValue(description);
                            headerRow.GetCell(columnIndex).CellStyle = headStyle;

                            //设置列宽  
                            sheet.SetColumnWidth(columnIndex, (arrColWidth[columnIndex] + 1) * 256);
                        }
                    }
                    #endregion

                    #region 自定义表头输出
                    if (customs != null && customs.Count > 0)
                    {
                        startRow = customs.Count + 1;
                        foreach (var _custom in customs)
                        {
                            for (int j = 0; j < _custom.Length; j++)
                            {
                                IRow headerRow = sheet.CreateRow(j + 2);
                                ICellStyle headStyle = workbook.CreateCellStyle();
                                headStyle.Alignment = HorizontalAlignment.Center;
                                IFont font = workbook.CreateFont();
                                font.FontHeightInPoints = 10;
#pragma warning disable CS0618 // '“IFont.Boldweight”已过时:“deprecated POI 3.15 beta 2. Use IsBold instead.”
                                font.Boldweight = 700;

                                headStyle.SetFont(font);

                                for (int _columnIndex = 0; _columnIndex < _custom[j].Length; _columnIndex++)
                                {
                                    headerRow.CreateCell(_columnIndex).SetCellValue(_custom[j][_columnIndex]);
                                    headerRow.GetCell(_columnIndex).CellStyle = headStyle;
                                }
                            }
                        }
                    }
                    #endregion
                }
                #endregion

                #region 填充内容
                ICellStyle contentStyle = workbook.CreateCellStyle();
                contentStyle.Alignment = HorizontalAlignment.Left;

                IRow dataRow = sheet.CreateRow(rowIndex + startRow); // 前面2行已被占用
                for (int columnIndex = 0; columnIndex < properties.Length; columnIndex++)
                {
                    ICell newCell = dataRow.CreateCell(columnIndex);
                    newCell.CellStyle = contentStyle;

                    string drValue = properties[columnIndex].GetValue(list[rowIndex], null).ParseToString();
                    switch (properties[columnIndex].PropertyType.ToString())
                    {
                        case "System.String":
                            newCell.SetCellValue(drValue);

                            break;

                        case "System.DateTime":
                        case "System.Nullable`1[System.DateTime]":
                            if (!string.IsNullOrEmpty(drValue))
                                newCell.SetCellValue(drValue.ParseToDateTime());
                            newCell.CellStyle = dateStyle; //格式化显示  
                            break;

                        case "System.Boolean":
                        case "System.Nullable`1[System.Boolean]":
                            newCell.SetCellValue(drValue.ParseToBool());
                            break;

                        case "System.Byte":
                        case "System.Nullable`1[System.Byte]":
                        case "System.Int16":
                        case "System.Nullable`1[System.Int16]":
                        case "System.Int32":
                        case "System.Nullable`1[System.Int32]":
                            newCell.SetCellValue(drValue.ParseToInt());
                            break;

                        case "System.Int64":
                        case "System.Nullable`1[System.Int64]":
                            newCell.SetCellValue(drValue.ParseToString());
                            break;

                        case "System.Double":
                        case "System.Nullable`1[System.Double]":
                            newCell.SetCellValue(drValue.ParseToDouble());
                            break;

                        case "System.Decimal":
                        case "System.Nullable`1[System.Decimal]":
                            newCell.SetCellValue(drValue);
                            break;

                        case "System.DBNull":
                            newCell.SetCellValue(string.Empty);
                            break;

                        default:
                            newCell.SetCellValue(string.Empty);
                            break;
                    }
                }
                #endregion
            }

            //列宽自适应，只对英文和数字有效
            //for (int i = 0; i <= properties.Length; i++)
            //{
            //    sheet.AutoSizeColumn(i);
            //}
            //列宽自适应
            for (int columnNum = 0; columnNum <= properties.Length; columnNum++)
            {
                int columnWidth = sheet.GetColumnWidth(columnNum) / 256;
                for (int rowNum = 1; rowNum <= sheet.LastRowNum; rowNum++)
                {
                    IRow currentRow;
                    //当前行未被使用过
                    if (sheet.GetRow(rowNum) == null)
                    {
                        currentRow = sheet.CreateRow(rowNum);
                    }
                    else
                    {
                        currentRow = sheet.GetRow(rowNum);
                    }

                    if (currentRow.GetCell(columnNum) != null)
                    {
                        ICell currentCell = currentRow.GetCell(columnNum);
                        int length = Encoding.Default.GetBytes(currentCell.ToString()).Length;
                        if (length > 100)
                        {
                            columnWidth = 100;
                            dateStyle.WrapText = true; // 启用换行
                        }
                        else if (columnWidth < length)
                        {
                            columnWidth = length;

                        }
                        
                    }
                }
                sheet.SetColumnWidth(columnNum, columnWidth * 256);
            }
            return workbook;
        }

        /// <summary>  
        /// List导出到Excel的MemoryStream  
        /// </summary>  
        /// <param name="list">数据源</param>  
        /// <param name="columns">需要导出的属性</param>  
        /// <param name="customs">自定义表头</param>
        private static MemoryStream CreateExportMemoryStream(List<T> list, string[] columns, IList<string[]> customs)
        {
            XSSFWorkbook workbook = CreateExportXss(list, columns, customs);

            using (MemoryStream ms = new MemoryStream())
            {
                workbook.Write(ms, true);
                workbook.Close();
                ms.Flush();
                ms.Position = 0;
                return ms;
            }
        }


        /// <summary>
        /// 查找Excel列名对应的实体属性
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        private static PropertyInfo MapPropertyInfo(string columnName)
        {
            PropertyInfo[] propertyList = GetProperties(typeof(T));
            PropertyInfo propertyInfo = propertyList.Where(p => p.Name == columnName).FirstOrDefault();
            if (propertyInfo != null)
            {
                return propertyInfo;
            }
            else
            {
                foreach (PropertyInfo tempPropertyInfo in propertyList)
                {
                    DescriptionAttribute[] attributes = (DescriptionAttribute[])tempPropertyInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    if (attributes.Length > 0)
                    {
                        if (attributes[0].Description == columnName)
                        {
                            return tempPropertyInfo;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 得到类里面的属性集合
        /// </summary>
        /// <param name="type"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        private static PropertyInfo[] GetProperties(Type type, string[] columns = null)
        {
            PropertyInfo[] properties = null;
            if (dictCache.ContainsKey(type.FullName))
            {
                properties = dictCache[type.FullName] as PropertyInfo[];
            }
            else
            {
                properties = type.GetProperties();
                dictCache.TryAdd(type.FullName, properties);
            }

            if (columns != null && columns.Length > 0)
            {
                //  按columns顺序返回属性
                var columnPropertyList = new List<PropertyInfo>();
                foreach (var column in columns)
                {
                    var columnProperty = properties.Where(p => p.Name == column).FirstOrDefault();
                    if (columnProperty != null)
                    {
                        columnPropertyList.Add(columnProperty);
                    }
                }
                return columnPropertyList.ToArray();
            }
            else
            {
                return properties;
            }
        }
        #endregion

        #region Csv

        public static byte[] ExportToCSV(List<T> list, string[] columns, IList<string[]> customs = null)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            Encoding _encode = Encoding.GetEncoding("gb2312");
            StringBuilder sb = new StringBuilder();
            int i = 0;
            for (i = 0; i < columns.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }
                sb.Append("\"" + columns[i].ToString().Replace("\"", "\"\"") + "\"");
            }
            sb.AppendLine("");
            if (customs != null && customs.Count > 0)
            {
                foreach (var item in customs)
                {
                    for (i = 0; i < item.Length; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(",");
                        }
                        sb.Append("\"" + item[i].ToString().Replace("\"", "\"\"") + "\"");
                    }
                    sb.AppendLine("");
                }
            }

            if (list != null && list.Count > 0)
            {
                Type type = typeof(T);
                PropertyInfo[] properties = GetProperties(type, columns);

                for (int rowIndex = 0; rowIndex < list.Count; rowIndex++)
                {
                    for (int columnIndex = 0; columnIndex < properties.Length; columnIndex++)
                    {
                        if (columnIndex > 0)
                        {
                            sb.Append(",");
                        }
                        string drValue = properties[columnIndex].GetValue(list[rowIndex], null).ParseToString();
                        #region 赋值
                        switch (properties[columnIndex].PropertyType.ToString())
                        {
                            case "System.String":
                                sb.Append("\"" + drValue.ToString().Replace("\"", "\"\"") + "\"");
                                break;

                            case "System.DateTime":
                            case "System.Nullable`1[System.DateTime]":
                                sb.Append("\"" + drValue.ParseToDateTime() + "\"");
                                break;

                            case "System.Boolean":
                            case "System.Nullable`1[System.Boolean]":
                                sb.Append("\"" + drValue.ParseToBool() + "\"");
                                break;

                            case "System.Byte":
                            case "System.Nullable`1[System.Byte]":
                            case "System.Int16":
                            case "System.Nullable`1[System.Int16]":
                            case "System.Int32":
                            case "System.Nullable`1[System.Int32]":
                                sb.Append("\"" + drValue.ParseToInt() + "\"");
                                break;

                            case "System.Int64":
                            case "System.Nullable`1[System.Int64]":
                                sb.Append("\"" + drValue.ParseToString() + "\"");
                                break;

                            case "System.Double":
                            case "System.Nullable`1[System.Double]":
                                sb.Append("\"" + drValue.ParseToDouble() + "\"");
                                break;

                            case "System.Decimal":
                            case "System.Nullable`1[System.Decimal]":
                                sb.Append("\"" + drValue + "\"");
                                break;
                            case "System.DBNull":
                                sb.Append("");
                                break;

                            default:
                                sb.Append("");
                                break;
                        }
                        #endregion
                    }
                    sb.AppendLine("");
                }
            }


            byte[] bytes = new byte[0];
            using (MemoryStream stream = new MemoryStream(_encode.GetBytes(sb.ToString())))
            {
                bytes = stream.ToArray();
                stream.Close();
            }

            return bytes;
        }



        public static byte[] ExportToCSV(StringBuilder sb)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            Encoding _encode = Encoding.GetEncoding("gb2312");
            byte[] bytes = new byte[0];
            using (MemoryStream stream = new MemoryStream(_encode.GetBytes(sb.ToString())))
            {
                bytes = stream.ToArray();
                stream.Close();
            }

            return bytes;
        }

        #endregion


    }

}
