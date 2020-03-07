﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGJv3
{
    public class LevenshteinDistance
    {

        private static LevenshteinDistance _instance = null;
        public static LevenshteinDistance Instance
        {
            get
            {
                if (_instance == null)
                {
                    return new LevenshteinDistance();
                }
                return _instance;
            }
        }


        /// <summary>
                /// 取最小的一位数
                /// </summary>
                /// <param name="first"></param>
                /// <param name="second"></param>
                /// <param name="third"></param>
                /// <returns></returns>
        public int LowerOfThree(int first, int second, int third)
        {
            int min = first;
            if (second < min)
                min = second;
            if (third < min)
                min = third;
            return min;
        }

        public int Levenshtein_Distance(string str1, string str2)
        {
            int[,] Matrix;
            int n = str1.Length;
            int m = str2.Length;

            int temp = 0;
            char ch1;
            char ch2;
            int i = 0;
            int j = 0;
            if (n == 0)
            {
                return m;
            }
            if (m == 0)
            {

                return n;
            }
            Matrix = new int[n + 1, m + 1];

            for (i = 0; i <= n; i++)
            {
                //初始化第一列
                Matrix[i, 0] = i;
            }

            for (j = 0; j <= m; j++)
            {
                //初始化第一行
                Matrix[0, j] = j;
            }

            for (i = 1; i <= n; i++)
            {
                ch1 = str1[i - 1];
                for (j = 1; j <= m; j++)
                {
                    ch2 = str2[j - 1];
                    if (ch1.Equals(ch2))
                    {
                        temp = 0;
                    }
                    else
                    {
                        temp = 1;
                    }
                    Matrix[i, j] = LowerOfThree(Matrix[i - 1, j] + 1, Matrix[i, j - 1] + 1, Matrix[i - 1, j - 1] + temp);


                }
            }

            for (i = 0; i <= n; i++)
            {
                for (j = 0; j <= m; j++)
                {
                    Console.Write(" {0} ", Matrix[i, j]);
                }
                Console.WriteLine("");
            }
            return Matrix[n, m];

        }

        /// <summary>
        /// 计算字符串相似度
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        public decimal Compute(string str1, string str2)
        {
            int maxLenth = str1.Length > str2.Length ? str1.Length : str2.Length;
            int val = Levenshtein_Distance(str1, str2);
            return 1 - (decimal)val / maxLenth;
        }
    }
    //public class LevenshteinDistance
    //{
    //    #region 私有变量  
    //    /// <summary>  
    //    /// 字符串1  
    //    /// </summary>  
    //    private char[] _ArrChar1;
    //    /// <summary>  
    //    /// 字符串2  
    //    /// </summary>  
    //    private char[] _ArrChar2;
    //    /// <summary>  
    //    /// 统计结果  
    //    /// </summary>  
    //    private Result _Result;
    //    /// <summary>  
    //    /// 开始时间  
    //    /// </summary>  
    //    private DateTime _BeginTime;
    //    /// <summary>  
    //    /// 结束时间  
    //    /// </summary>  
    //    private DateTime _EndTime;
    //    /// <summary>  
    //    /// 计算次数  
    //    /// </summary>  
    //    private int _ComputeTimes;
    //    /// <summary>  
    //    /// 算法矩阵  
    //    /// </summary>  
    //    private int[,] _Matrix;
    //    /// <summary>  
    //    /// 矩阵列数  
    //    /// </summary>  
    //    private int _Column;
    //    /// <summary>  
    //    /// 矩阵行数  
    //    /// </summary>  
    //    private int _Row;
    //    #endregion
    //    #region 属性  
    //    public Result ComputeResult
    //    {
    //        get { return _Result; }
    //    }
    //    #endregion
    //    #region 构造函数  
    //    public LevenshteinDistance(string str1, string str2)
    //    {
    //        this.LevenshteinDistanceInit(str1, str2);
    //    }
    //    public LevenshteinDistance()
    //    {
    //    }
    //    #endregion
    //    #region 算法实现  
    //    /// <summary>  
    //    /// 初始化算法基本信息  
    //    /// </summary>  
    //    /// <param name="str1">字符串1</param>  
    //    /// <param name="str2">字符串2</param>  
    //    private void LevenshteinDistanceInit(string str1, string str2)
    //    {
    //        _ArrChar1 = str1.ToCharArray();
    //        _ArrChar2 = str2.ToCharArray();
    //        _Result = new Result();
    //        _ComputeTimes = 0;
    //        _Row = _ArrChar1.Length + 1;
    //        _Column = _ArrChar2.Length + 1;
    //        _Matrix = new int[_Row, _Column];
    //    }
    //    /// <summary>  
    //    /// 计算相似度  
    //    /// </summary>  
    //    public void Compute()
    //    {
    //        //开始时间  
    //        _BeginTime = DateTime.Now;
    //        //初始化矩阵的第一行和第一列  
    //        this.InitMatrix();
    //        int intCost = 0;
    //        for (int i = 1; i < _Row; i++)
    //        {
    //            for (int j = 1; j < _Column; j++)
    //            {
    //                if (_ArrChar1[i - 1] == _ArrChar2[j - 1])
    //                {
    //                    intCost = 0;
    //                }
    //                else
    //                {
    //                    intCost = 1;
    //                }
    //                //关键步骤，计算当前位置值为左边+1、上面+1、左上角+intCost中的最小值   
    //                //循环遍历到最后_Matrix[_Row - 1, _Column - 1]即为两个字符串的距离  
    //                _Matrix[i, j] = this.Minimum(_Matrix[i - 1, j] + 1, _Matrix[i, j - 1] + 1, _Matrix[i - 1, j - 1] + intCost);
    //                _ComputeTimes++;
    //            }
    //        }
    //        //结束时间  
    //        _EndTime = DateTime.Now;
    //        //相似率 移动次数小于最长的字符串长度的20%算同一题  
    //        int intLength = _Row > _Column ? _Row : _Column;
    //        _Result.Rate = (1 - (double)_Matrix[_Row - 1, _Column - 1] / (intLength - 1)).ToString().Substring(0, 6);
    //        if (_Result.Rate.Length > 6)
    //        {
    //            _Result.Rate = _Result.Rate.Substring(0, 6);
    //        }
    //        _Result.UseTime = (_EndTime - _BeginTime).ToString();
    //        _Result.ComputeTimes = _ComputeTimes.ToString() + " 距离为：" + _Matrix[_Row - 1, _Column - 1].ToString();
    //    }
    //    /// <summary>  
    //    /// 计算相似度  
    //    /// </summary>  
    //    /// <param name="str1">字符串1</param>  
    //    /// <param name="str2">字符串2</param>  
    //    public void Compute(string str1, string str2)
    //    {
    //        this.LevenshteinDistanceInit(str1, str2);
    //        this.Compute();
    //    }
    //    /// <summary>  
    //    /// 初始化矩阵的第一行和第一列  
    //    /// </summary>  
    //    private void InitMatrix()
    //    {
    //        for (int i = 0; i < _Column; i++)
    //        {
    //            _Matrix[0, i] = i;
    //        }
    //        for (int i = 0; i < _Row; i++)
    //        {
    //            _Matrix[i, 0] = i;
    //        }
    //    }
    //    /// <summary>  
    //    /// 取三个数中的最小值  
    //    /// </summary>  
    //    /// <param name="First"></param>  
    //    /// <param name="Second"></param>  
    //    /// <param name="Third"></param>  
    //    /// <returns></returns>  
    //    private int Minimum(int First, int Second, int Third)
    //    {
    //        int intMin = First;
    //        if (Second < intMin)
    //        {
    //            intMin = Second;
    //        }
    //        if (Third < intMin)
    //        {
    //            intMin = Third;
    //        }
    //        return intMin;
    //    }
    //    #endregion
    //}
    ///// <summary>  
    ///// 计算结果  
    ///// </summary>  
    //public struct Result
    //{
    //    /// <summary>  
    //    /// 相似度  
    //    /// </summary>  
    //    public string Rate;
    //    /// <summary>  
    //    /// 对比次数  
    //    /// </summary>  
    //    public string ComputeTimes;
    //    /// <summary>  
    //    /// 使用时间  
    //    /// </summary>  
    //    public string UseTime;
    //}
}
