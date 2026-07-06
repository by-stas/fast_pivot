export interface PivotResponse {
  weekStart: string;
  weekEnd: string;
  days: PivotDay[];
  testSuites: string[];
  rows: PivotRow[];
  warnings: PivotWarning[];
}

export interface PivotDay {
  date: string;
  label: string;
}

export interface PivotRow {
  project: string;
  product: string;
  version: string;
  cells: Record<string, PivotCellItem[]>;
}

export interface PivotCellItem {
  title: string;
  description: string;
  machine: string;
  image: string;
  test: string;
  testSuite: string;
  scheduledTime: string;
}

export interface PivotWarning {
  title: string;
  crontab: string;
  message: string;
}
