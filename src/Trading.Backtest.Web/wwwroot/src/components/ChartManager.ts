import type { EquityPoint, PeriodMetrics } from '../types/api';
import { Chart, ChartConfiguration, registerables } from 'chart.js';

// 注册Chart.js组件
Chart.register(...registerables);

/**
 * 图表管理器
 */
export class ChartManager {
  private equityChart: Chart | null = null;
  private yearlyChart: Chart | null = null;
  private monthlyChart: Chart | null = null;

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
          data: data.map(d => d.equity),
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
            label: '年度收益率 (%)',
            data: data.map(d => d.return * 100),
            backgroundColor: data.map(d => 
              d.return >= 0 ? 'rgba(75, 192, 192, 0.6)' : 'rgba(255, 99, 132, 0.6)'
            ),
            borderColor: data.map(d => 
              d.return >= 0 ? 'rgb(75, 192, 192)' : 'rgb(255, 99, 132)'
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
                  `收益率: ${(item.return * 100).toFixed(2)}%`,
                  `收益: ${item.profit.toFixed(2)}`,
                  `交易数: ${item.trades}`,
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
              text: '收益率 (%)'
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
            label: '月度收益率 (%)',
            data: data.map(d => d.return * 100),
            backgroundColor: data.map(d => 
              d.return >= 0 ? 'rgba(75, 192, 192, 0.6)' : 'rgba(255, 99, 132, 0.6)'
            ),
            borderColor: data.map(d => 
              d.return >= 0 ? 'rgb(75, 192, 192)' : 'rgb(255, 99, 132)'
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
                  `收益率: ${(item.return * 100).toFixed(2)}%`,
                  `收益: ${item.profit.toFixed(2)}`,
                  `交易数: ${item.trades}`,
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
              text: '收益率 (%)'
            }
          }
        }
      }
    };

    this.monthlyChart = new Chart(canvas, config);
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
  }
}

// 全局实例
export const chartManager = new ChartManager();
