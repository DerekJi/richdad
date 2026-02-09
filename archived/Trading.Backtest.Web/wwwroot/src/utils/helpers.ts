/**
 * 安全地格式化数字
 */
export function formatNumber(value: number | null | undefined, decimals: number = 2): string {
  if (value === null || value === undefined || isNaN(value)) {
    return '0.00';
  }
  return parseFloat(value.toString()).toFixed(decimals);
}

/**
 * 格式化日期范围
 */
export function formatDateRange(start?: string, end?: string): string {
  if (!start || !end) return '';
  if (start === end) return `<div class="metric-date">${start}</div>`;
  return `<div class="metric-date">${start} ~ ${end}</div>`;
}

/**
 * 获取元素值（兼容大小写）
 */
export function getPropertyValue<T>(obj: any, key: string): T {
  return obj[key] || obj[key.charAt(0).toUpperCase() + key.slice(1)];
}
