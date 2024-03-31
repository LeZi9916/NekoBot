using SkiaSharp;
using System.Collections.Generic;
using System;
using ZXing.SkiaSharp;
using System.IO;
using static TelegramBot.Image;
using System.Linq;
using System.Numerics;

namespace TelegramBot
{
    class ChartHelper
    {
        public class KNode
        {
            /// <summary>
            /// Node的时间点
            /// </summary>
            public DateTime Date { get; set; }
            /// <summary>
            /// 开盘价
            /// </summary>
            public float Open { get; set; }
            /// <summary>
            /// 最高价
            /// </summary>
            public float High { get; set; }
            /// <summary>
            /// 最低价
            /// </summary>
            public float Low { get; set; }
            /// <summary>
            /// 收盘价
            /// </summary>
            public float Close { get; set; }
        }
        public class CandlestickChartHelper<TX,TY> 
            where TX: struct,IComparable, IComparable<TX>,
                                           IConvertible, IEquatable<TX>, IFormattable 
            where TY: struct, IComparable, IComparable<TY>,
                                           IConvertible, IEquatable<TY>, IFormattable
        {
            
            /// <summary>
            /// Top、Buttom、Left、Right
            /// </summary>
            public int[] Margin { get; private set; } = { 50,50,50,50 };
            /// <summary>
            /// 画幅横向长度
            /// </summary>
            public int XSize { get; set; } = 1920;
            /// <summary>
            /// 画幅纵向长度
            /// </summary>
            public int YSize { get; set; } = 1080;

            IList<TX> XSamples;
            IList<TY> YSamples;
            IList<KNode> Nodes;
            List<float> XPos;
            List<float> YPos;
            public CandlestickChartHelper(IList<KNode> nodes, IList<TX> XSamples, IList<TY> YSamples)
            {
                this.XSamples = XSamples;
                this.YSamples = YSamples;
                Nodes = nodes;
            }
            /// <summary>
            /// 绘制K线图
            /// </summary>
            public void Draw(string filePath)
            {
                var surface = SKSurface.Create(new SKImageInfo(XSize, YSize));
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                DrawAxis(canvas);
                DrawBody(canvas);

                SKImage img = surface.Snapshot();
                SKData data = img.Encode(SKEncodedImageFormat.Png, 100);
                FileStream stream = File.OpenWrite(filePath);
                data.SaveTo(stream);
                stream.Close();
            }
            /// <summary>
            /// 刻度线
            /// </summary>
            /// <param name="canvas"></param>
            /// <param name="width"></param>
            /// <param name="height"></param>
            void DrawAxis(SKCanvas canvas, int lineWidth = 2)
            {
                int length = 4;
                int XLength = XSize - (Margin[2] + Margin[3]);
                int YLength = YSize - (Margin[0] + Margin[1]);

                var pen = new SKPaint { Color = SKColors.Black, StrokeWidth = lineWidth };
                var textPen = new SKPaint 
                { 
                    Color = SKColors.Black, 
                    StrokeWidth = 2 ,
                    IsAntialias = true,
                    TextSize = 16,
                    TextAlign = SKTextAlign.Center
                };
                var scalePen = new SKPaint { Color = SKColors.Gray, StrokeWidth = 0.5f };
                XPos = new(); // X轴刻度坐标
                YPos = new(); // Y轴刻度坐标

                // 上边界坐标为 (x,Margin[0])
                // 下边界坐标为 (x, YSize - Margin[1])
                // 左边界坐标为 (Margin[2], y)
                // 右边界坐标为 (XSize - Margin[3], y)

                canvas.DrawLine(Margin[2], YSize - Margin[1], XSize - Margin[3], YSize - Margin[1], pen); // X轴
                canvas.DrawLine(Margin[2], YSize - Margin[1], Margin[2], Margin[0], pen); // Y轴

                if(XSamples is not null)
                {
                    var yPos = YSize - Margin[1];

                    var _XLength = (float)XLength / (XSamples.Count + 1); // 刻度之间的间隔
                    int distance = 14; // 文本与刻度线的距离
                    for (int i = 1; i <= XSamples.Count; i++) XPos.Add(i * _XLength);

                    for(int i =0;i <XSamples.Count;i++)
                    {
                        //canvas.DrawLine(XPos[i], Margin[1], XPos[i], YSize - Margin[0], scalePen);
                        canvas.DrawText(XSamples[i].ToString(), XPos[i] , yPos + distance, textPen);
                    }                    
                }
                if(YSamples is not null)
                {
                    var _YLength = (float)YLength / (YSamples.Count + 1); // 刻度之间的间隔
                    int distance = 20; // 文本与刻度线的距离
                    for (int i = 1; i <= YSamples.Count; i++) YPos.Add(i * _YLength);

                    for (int i = 0; i < YSamples.Count; i++)
                    {
                        //canvas.DrawLine(Margin[2], YPos[i], XSize - Margin[3], YPos[i], scalePen);
                        canvas.DrawText(YSamples[i].ToString(), Margin[2] - distance, YPos[i], textPen);
                    }
                }

            }
            /// <summary>
            /// Body绘制
            /// </summary>
            /// <param name="canvas"></param>
            /// <param name="data"></param>
            /// <param name="width"></param>
            /// <param name="height"></param>
            void DrawBody(SKCanvas canvas)
            {
                float xIncrement;

                if (XPos.Count > 1)
                    xIncrement = XPos[1] - XPos[0];
                else
                    xIncrement = XSize;

                

                for(int i = 0;i < Nodes.Count;i++)
                {
                    var item = Nodes[i];
                    float xPosition = XPos[i];

                    float openY = CalYPos(item.Open);   // 开盘点坐标
                    float closeY = CalYPos(item.Close); // 收盘点坐标
                    float highY = CalYPos(item.High);   // 最高点坐标
                    float lowY = CalYPos(item.Low);     // 最低点坐标
                    SKPaint paint = new SKPaint()
                    {
                        Color = item.Close > item.Open ? SKColors.Green : SKColors.Red,// 绿升红跌模式
                        IsStroke = true,
                       
                    };
                    var length = Math.Abs(openY - closeY);
                    var width = xIncrement * 0.8f;
                    canvas.DrawRect(xPosition - width /2, Math.Min(openY, closeY),width ,length, paint);
                    canvas.DrawLine(xPosition, highY, xPosition, lowY, paint);

                }
            }
            float CalYPos(float value)
            {
                dynamic maxValue = YSamples.Max();
                var pos = YPos.Max();
                var i = value / maxValue;

                return i * pos;
            }
            /// <summary>
            /// 设置边框距离
            /// </summary>
            /// <param name="top"></param>
            /// <param name="buttom"></param>
            /// <param name="left"></param>
            /// <param name="right"></param>
            public void SetMargin(int top, int buttom, int left, int right) => Margin = new int[] { top, buttom, left, right };
            /// <summary>
            /// 按百分比设置边框距离
            /// </summary>
            /// <param name="top"></param>
            /// <param name="buttom"></param>
            /// <param name="left"></param>
            /// <param name="right"></param>
            public void SetMargin(float top, float buttom, float left, float right)
            {
                int tMargin = (int)(YSize * top / 2);
                int bMargin = (int)(YSize * buttom / 2);
                int lMargin = (int)(XSize * left / 2);
                int rMargin = (int)(XSize * right / 2);
                Margin = new int[] { tMargin, bMargin, lMargin, rMargin };
            }
        }
        
    }
    internal static class Image
    {
        public class KLineData
        {
            public DateTime Date { get; set; }
            public float Open { get; set; }
            public float High { get; set; }
            public float Low { get; set; }
            public float Close { get; set; }
        }
        static string Decode(SKBitmap captured)
        {
            var barcodeReader = new BarcodeReader();
            var result = barcodeReader.Decode(captured);
            if (result is null)
                return null;
            else
                return result.Text;
        }
        static void DrawKLineChart(List<KLineData> data, string filePath)
        {
            int width = 800;
            int height = 600;
            using (SKSurface surface = SKSurface.Create(new SKImageInfo(width, height)))
            {
                SKCanvas canvas = surface.Canvas;
                canvas.Clear(SKColors.DarkGray);

                DrawAxis(canvas, width, height);
                DrawKLines(canvas, data, width, height);

            }
        }

        static void DrawAxis(SKCanvas canvas, int width, int height)
        {
            using (SKPaint paint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2 })
            {
                // 绘制X轴
                canvas.DrawLine(50, height - 50, width - 50, height - 50, paint);
                // 绘制Y轴
                canvas.DrawLine(50, height - 50, 50, 50, paint);
            }
        }

        static void DrawKLines(SKCanvas canvas, List<KLineData> data, int width, int height)
        {
            float xIncrement = (width - 100) / (float)data.Count;
            float xPosition = 60;

            foreach (var item in data)
            {
                // 这里简化了计算过程，实际应用中应根据数据动态计算Y轴的比例
                float openY = height - 50 - item.Open;
                float closeY = height - 50 - item.Close;
                float highY = height - 50 - item.High;
                float lowY = height - 50 - item.Low;

                using (SKPaint paint = new SKPaint())
                {
                    paint.Color = item.Close > item.Open ? SKColors.Green : SKColors.Red;
                    paint.IsStroke = true;

                    // 绘制蜡烛
                    canvas.DrawRect(xPosition, Math.Min(openY, closeY), 10, Math.Abs(openY - closeY), paint);

                    // 绘制影线
                    canvas.DrawLine(xPosition + 5, highY, xPosition + 5, lowY, paint);
                }

                xPosition += xIncrement;
            }
        }
        public static void Test()
        {
            List<KLineData> data = new List<KLineData>
        {
            new KLineData { Date = DateTime.Now.AddDays(-4), Open = 100, High = 110, Low = 90, Close = 105 },
            new KLineData { Date = DateTime.Now.AddDays(-3), Open = 105, High = 115, Low = 95, Close = 110 },
            new KLineData { Date = DateTime.Now.AddDays(-2), Open = 110, High = 120, Low = 100, Close = 115 },
            new KLineData { Date = DateTime.Now.AddDays(-1), Open = 115, High = 125, Low = 105, Close = 120 },
            new KLineData { Date = DateTime.Now, Open = 120, High = 130, Low = 110, Close = 125 },
        };

            DrawKLineChart(data, "KLineChart.png");
            Console.WriteLine("K线图已保存为KLineChart.png");
        }
        public static string FromFile(string imagePath) => Decode(SKBitmap.Decode(imagePath));
    }
}
