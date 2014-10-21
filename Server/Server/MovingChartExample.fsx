#r "System.Windows.Forms.DataVisualization.dll"

open System
open System.Drawing
open System.Threading
open System.Windows.Forms
open System.Windows.Forms.DataVisualization.Charting

/// Add data series of the specified chart type to a chart
let createSeries typ (chart:Chart) =
    let series = new Series(ChartType = typ)
    chart.Series.Add(series)    
    series

/// Create form with chart and add the first chart series
let createChart() =
    let chart = new Chart(Dock = DockStyle.Fill, 
                          Palette = ChartColorPalette.Pastel)
    let mainForm = new Form(Visible = true, Width = 700, Height = 500)
    let area = new ChartArea()
    area.AxisX.MajorGrid.LineColor <- Color.LightGray
    area.AxisY.MajorGrid.LineColor <- Color.LightGray
    mainForm.Controls.Add(chart)
    chart.ChartAreas.Add(area)
    chart

let numbers = 
    seq { while true do for i in 0.0 .. 0.1 .. Double.MaxValue do yield i }
    |> Seq.map sin

let chart = createChart()
let series = createSeries SeriesChartType.FastLine chart

let firstWindow = numbers |> Seq.take 100
    
for number in firstWindow do
    series.Points.AddY(number) |> ignore

let context = SynchronizationContext.Current
let numbersEnumerator = numbers.GetEnumerator()
async{
    do! Async.SwitchToContext context
    while(numbersEnumerator.MoveNext() && not chart.IsDisposed) do        
        let number = numbersEnumerator.Current
        series.Points.SuspendUpdates()
        series.Points.RemoveAt(0)
        series.Points.AddY( number ) |> ignore
        series.Points.ResumeUpdates()
        do! Async.SwitchToThreadPool()
        do! Async.Sleep(100)
        do! Async.SwitchToContext context
}
|> Async.Start
