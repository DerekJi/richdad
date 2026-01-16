import type { EquityPoint, PeriodMetrics, TimeSlot } from '../types/api';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import annotationPlugin from 'chartjs-plugin-annotation';
import { CandlestickController, CandlestickElement } from 'chartjs-chart-financial';
import zoomPlugin from 'chartjs-plugin-zoom';

// 注册Chart.js组件
Chart.register(...registerables, annotationPlugin, CandlestickController, CandlestickElement, zoomPlugin);

/**
 * 图表管理器
 */
export class ChartManager {
  private equityChart: Chart | null = null;
  private yearlyChart: Chart | null = null;
  private monthlyChart: Chart | null = null;
  private profitSlotChart: Chart | null = null;
  private lossSlotChart: Chart | null = null;
  private tradeKlineChart: Chart | null = null;

  /**
   * 绘制权益曲线
   */
  drawEquityChart(data: EquityPoint[]): void {
    if (this.equityChart) {
      this.equityChart.destroy();
    }

    const canvas = document.getElementById('equityChart') as HTMLCanvasElement;
    if (!canvas) return;

    const config: ChartConfiguration = {
      type: 'line',
      data: {
        labels: data.map(d => d.time),
        datasets: [{
          label: '账户权益',
          data: data.map(d => d.cumulativeProfit),
          borderColor: '#4a90e2',
          backgroundColor: 'rgba(74, 144, 226, 0.1)',
          tension: 0.1,
          fill: true
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: true,
            position: 'top'
          },
          tooltip: {
            mode: 'index',
            intersect: false
          }
        },
        scales: {
          x: {
            display: true,
            title: {
              display: true,
              text: '时间'
            },
            ticks: {
              maxRotation: 45,
              minRotation: 45,
              autoSkip: true,
              maxTicksLimit: 20
            }
          },
          y: {
            display: true,
            title: {
              display: true,
              text: '权益'
            }
          }
        }
      }
    };

    this.equityChart = new Chart(canvas, config);
  }

  /**
   * 绘制年度统计图表
   */
  drawYearlyChart(data: PeriodMetrics[]): void {
    if (this.yearlyChart) {
      this.yearlyChart.destroy();
    }

    const canvas = document.getElementById('yearlyChart') as HTMLCanvasElement;
    if (!canvas) return;

    const config: ChartConfiguration = {
      type: 'bar',
      data: {
        labels: data.map(d => d.period),
        datasets: [
          {
            label: '年度收益 (USD)',
            data: data.map(d => d.profitLoss),
            backgroundColor: data.map(d =>
              d.profitLoss >= 0 ? 'rgba(75, 192, 192, 0.6)' : 'rgba(255, 99, 132, 0.6)'
            ),
            borderColor: data.map(d =>
              d.profitLoss >= 0 ? 'rgb(75, 192, 192)' : 'rgb(255, 99, 132)'
            ),
            borderWidth: 1
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: true,
            position: 'top'
          },
          tooltip: {
            callbacks: {
              label: (context) => {
                const index = context.dataIndex;
                const item = data[index];
                return [
                  `收益: ${item.profitLoss.toFixed(2)}`,
                  `交易数: ${item.tradeCount}`,
                  `胜率: ${(item.winRate * 100).toFixed(2)}%`
                ];
              }
            }
          }
        },
        scales: {
          x: {
            display: true,
            title: {
              display: true,
              text: '年份'
            }
          },
          y: {
            display: true,
            title: {
              display: true,
              text: '收益 (USD)'
            }
          }
        }
      }
    };

    this.yearlyChart = new Chart(canvas, config);
  }

  /**
   * 绘制月度统计图表
   */
  drawMonthlyChart(data: PeriodMetrics[]): void {
    if (this.monthlyChart) {
      this.monthlyChart.destroy();
    }

    const canvas = document.getElementById('monthlyChart') as HTMLCanvasElement;
    if (!canvas) return;

    const config: ChartConfiguration = {
      type: 'bar',
      data: {
        labels: data.map(d => d.period),
        datasets: [
          {
            label: '月度收益 (USD)',
            data: data.map(d => d.profitLoss),
            backgroundColor: data.map(d =>
              d.profitLoss >= 0 ? 'rgba(75, 192, 192, 0.6)' : 'rgba(255, 99, 132, 0.6)'
            ),
            borderColor: data.map(d =>
              d.profitLoss >= 0 ? 'rgb(75, 192, 192)' : 'rgb(255, 99, 132)'
            ),
            borderWidth: 1
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: true,
            position: 'top'
          },
          tooltip: {
            callbacks: {
              label: (context) => {
                const index = context.dataIndex;
                const item = data[index];
                return [
                  `收益: ${item.profitLoss.toFixed(2)}`,
                  `交易数: ${item.tradeCount}`,
                  `胜率: ${(item.winRate * 100).toFixed(2)}%`
                ];
              }
            }
          }
        },
        scales: {
          x: {
            display: true,
            title: {
              display: true,
              text: '月份'
            },
            ticks: {
              maxRotation: 90,
              minRotation: 90,
              autoSkip: true,
              maxTicksLimit: 24
            }
          },
          y: {
            display: true,
            title: {
              display: true,
              text: '收益 (USD)'
            }
          }
        }
      }
    };

    this.monthlyChart = new Chart(canvas, config);
  }

  /**
   * 绘制时间段盈亏图表
   */
  drawTimeSlotCharts(profitSlots: TimeSlot[], lossSlots: TimeSlot[]): void {
    this.drawProfitSlotChart(profitSlots);
    this.drawLossSlotChart(lossSlots);
  }

  /**
   * 绘制盈利时间段TOP5
   */
  private drawProfitSlotChart(data: TimeSlot[]): void {
    if (this.profitSlotChart) {
      this.profitSlotChart.destroy();
    }

    const canvas = document.getElementById('profitSlotChart') as HTMLCanvasElement;
    if (!canvas) return;

    const config: ChartConfiguration = {
      type: 'bar',
      data: {
        labels: data.map(d => d.timeSlot),
        datasets: [{
          label: '总盈利 (USD)',
          data: data.map(d => d.totalProfitLoss),
          backgroundColor: 'rgba(75, 192, 192, 0.6)',
          borderColor: 'rgb(75, 192, 192)',
          borderWidth: 1
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: true,
            position: 'top'
          },
          tooltip: {
            callbacks: {
              label: (context) => {
                const index = context.dataIndex;
                const item = data[index];
                return [
                  `总盈利: ${item.totalProfitLoss.toFixed(2)}`,
                  `交易数: ${item.tradeCount}`,
                  `平均: ${item.avgProfitLoss.toFixed(2)}`,
                  `胜率: ${item.winRate.toFixed(2)}%`
                ];
              }
            }
          }
        },
        scales: {
          x: {
            display: true,
            title: {
              display: true,
              text: '时间段 (UTC)'
            }
          },
          y: {
            display: true,
            title: {
              display: true,
              text: '盈利 (USD)'
            }
          }
        }
      }
    };

    this.profitSlotChart = new Chart(canvas, config);
  }

  /**
   * 绘制亏损时间段TOP5
   */
  private drawLossSlotChart(data: TimeSlot[]): void {
    if (this.lossSlotChart) {
      this.lossSlotChart.destroy();
    }

    const canvas = document.getElementById('lossSlotChart') as HTMLCanvasElement;
    if (!canvas) return;

    const config: ChartConfiguration = {
      type: 'bar',
      data: {
        labels: data.map(d => d.timeSlot),
        datasets: [{
          label: '总亏损 (USD)',
          data: data.map(d => d.totalProfitLoss),
          backgroundColor: 'rgba(255, 99, 132, 0.6)',
          borderColor: 'rgb(255, 99, 132)',
          borderWidth: 1
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: true,
            position: 'top'
          },
          tooltip: {
            callbacks: {
              label: (context) => {
                const index = context.dataIndex;
                const item = data[index];
                return [
                  `总亏损: ${item.totalProfitLoss.toFixed(2)}`,
                  `交易数: ${item.tradeCount}`,
                  `平均: ${item.avgProfitLoss.toFixed(2)}`,
                  `胜率: ${item.winRate.toFixed(2)}%`
                ];
              }
            }
          }
        },
        scales: {
          x: {
            display: true,
            title: {
              display: true,
              text: '时间段 (UTC)'
            }
          },
          y: {
            display: true,
            title: {
              display: true,
              text: '亏损 (USD)'
            }
          }
        }
      }
    };

    this.lossSlotChart = new Chart(canvas, config);
  }

  /**
   * 销毁所有图表
   */
  destroyAll(): void {
    if (this.equityChart) {
      this.equityChart.destroy();
      this.equityChart = null;
    }
    if (this.yearlyChart) {
      this.yearlyChart.destroy();
      this.yearlyChart = null;
    }
    if (this.monthlyChart) {
      this.monthlyChart.destroy();
      this.monthlyChart = null;
    }
    if (this.profitSlotChart) {
      this.profitSlotChart.destroy();
      this.profitSlotChart = null;
    }
    if (this.lossSlotChart) {
      this.lossSlotChart.destroy();
      this.lossSlotChart = null;
    }
    if (this.tradeKlineChart) {
      this.tradeKlineChart.destroy();
      this.tradeKlineChart = null;
    }
  }

  /**
   * 绘制单笔交易的K线图（需要后端API支持）
   * 暂时显示提示信息
   */
  async drawTradeKlineChart(
    tradeIndex: number,
    symbol: string,
    trade: any,
    config: any
  ): Promise<void> {
    const modal = document.getElementById('tradeChartModal');
    const title = document.getElementById('tradeChartTitle');

    if (!modal || !title) return;

    title.textContent = `交易 #${tradeIndex + 1} - K线图 (${symbol})`;
    modal.style.display = 'flex';

    try {
      // 导入backtestService
      const { backtestService } = await import('../services/backtestService');

      // 调用API获取K线数据
      const response = await backtestService.getTradeKlines({
        symbol: symbol,
        csvFilter: config.csvFilter || '',
        openTime: trade.openTime,
        closeTime: trade.closeTime,
        openPrice: trade.openPrice,
        closePrice: trade.closePrice,
        stopLoss: trade.stopLoss,
        takeProfit: trade.takeProfit,
        direction: trade.direction,
        baseEma: config.baseEma || 200,
        atrPeriod: config.atrPeriod || 14,
        emaList: config.emaList || [20, 60, 80, 100, 200]
      });

      this.renderKlineChart(response);
    } catch (error) {
      console.error('Failed to load kline data:', error);
      const canvas = document.getElementById('tradeKlineChart') as HTMLCanvasElement;
      if (canvas) {
        const ctx = canvas.getContext('2d');
        if (ctx) {
          ctx.clearRect(0, 0, canvas.width, canvas.height);
          ctx.font = '16px Arial';
          ctx.textAlign = 'center';
          ctx.fillStyle = '#f44336';
          ctx.fillText('加载K线数据失败', canvas.width / 2, canvas.height / 2);
          ctx.fillText(String(error), canvas.width / 2, canvas.height / 2 + 25);
        }
      }
    }
  }

  /**
   * 渲染K线图和EMA
   */
  private renderKlineChart(data: any): void {
    if (this.tradeKlineChart) {
      this.tradeKlineChart.destroy();
    }

    const canvas = document.getElementById('tradeKlineChart') as HTMLCanvasElement;
    if (!canvas) return;

    // 准备数据
    const labels = data.candles.map((c: any) => c.dateTime);

    // 定义EMA颜色
    const emaColors: { [key: number]: string } = {
      20: '#FF6384',
      60: '#36A2EB',
      80: '#FFCE56',
      100: '#4BC0C0',
      200: '#9966FF'
    };

    // 准备EMA数据集
    const emaDatasets = Object.keys(data.emaData).map(period => ({
      label: `EMA${period}`,
      data: data.emaData[period],
      borderColor: emaColors[parseInt(period)] || '#999',
      borderWidth: 1.5,
      fill: false,
      pointRadius: 0,
      tension: 0.1,
      type: 'line' as const,
      order: 1
    }));

    // K线数据（使用OHLC格式）
    const candleDataset = {
      label: 'Price',
      data: data.candles.map((c: any) => ({
        x: c.dateTime,
        o: c.open,
        h: c.high,
        l: c.low,
        c: c.close
      })),
      borderColor: data.candles.map((c: any) =>
        c.close >= c.open ? 'rgba(76, 175, 80, 1)' : 'rgba(244, 67, 54, 1)'
      ),
      backgroundColor: data.candles.map((c: any) =>
        c.close >= c.open ? 'rgba(76, 175, 80, 0.5)' : 'rgba(244, 67, 54, 0.5)'
      ),
      type: 'candlestick' as any,
      order: 2
    };

    const config: ChartConfiguration = {
      type: 'line',
      data: {
        labels: labels,
        datasets: [
          ...emaDatasets,
          candleDataset
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: {
          mode: 'index',
          intersect: false
        },
        plugins: {
          legend: {
            display: true,
            position: 'top'
          },
          tooltip: {
            mode: 'index',
            callbacks: {
              label: (context) => {
                const index = context.dataIndex;
                const candle = data.candles[index];
                if (context.dataset.label === 'Price') {
                  const labels = [
                    `开: ${candle.open.toFixed(2)}`,
                    `高: ${candle.high.toFixed(2)}`,
                    `低: ${candle.low.toFixed(2)}`,
                    `收: ${candle.close.toFixed(2)}`
                  ];
                  if (candle.atr && candle.atr > 0) {
                    labels.push(`ATR: ${candle.atr.toFixed(2)}`);
                  }
                  return labels;
                }
                return `${context.dataset.label}: ${context.parsed.y?.toFixed(2)}`;
              }
            }
          },
          annotation: {
            annotations: {
              openLine: {
                type: 'line',
                xMin: data.openIndex,
                xMax: data.openIndex,
                borderColor: 'green',
                borderWidth: 2,
                label: {
                  display: true,
                  content: '开仓',
                  position: 'start'
                }
              },
              closeLine: {
                type: 'line',
                xMin: data.closeIndex,
                xMax: data.closeIndex,
                borderColor: 'red',
                borderWidth: 2,
                label: {
                  display: true,
                  content: '平仓',
                  position: 'start'
                }
              },
              stopLossLine: {
                type: 'line',
                yMin: data.stopLoss,
                yMax: data.stopLoss,
                borderColor: 'red',
                borderWidth: 1,
                borderDash: [5, 5],
                label: {
                  display: true,
                  content: '止损',
                  position: 'end'
                }
              },
              takeProfitLine: {
                type: 'line',
                yMin: data.takeProfit,
                yMax: data.takeProfit,
                borderColor: 'green',
                borderWidth: 1,
                borderDash: [5, 5],
                label: {
                  display: true,
                  content: '止盈',
                  position: 'end'
                }
              }
            }
          },
          zoom: {
            zoom: {
              wheel: {
                enabled: true,
                speed: 0.1
              },
              pinch: {
                enabled: true
              },
              mode: 'x'
            },
            pan: {
              enabled: true,
              mode: 'x'
            },
            limits: {
              x: { min: 'original', max: 'original' }
            }
          }
        },
        scales: {
          x: {
            display: true,
            ticks: {
              maxRotation: 45,
              minRotation: 45,
              autoSkip: true,
              maxTicksLimit: 20
            }
          },
          y: {
            display: true,
            position: 'right',
            min: Math.min(...data.candles.map((c: any) => c.low)) * 0.998,
            max: Math.max(...data.candles.map((c: any) => c.high)) * 1.002
          }
        }
      }
    };

    this.tradeKlineChart = new Chart(canvas, config);
  }
}

// 全局实例
export const chartManager = new ChartManager();
