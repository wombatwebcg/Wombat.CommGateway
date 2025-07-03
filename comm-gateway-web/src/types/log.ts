export enum LogLevel {
  INFO = 'INFO',
  WARN = 'WARN',
  ERROR = 'ERROR',
  DEBUG = 'DEBUG'
}

export interface LogEntry {
  id: string;
  timestamp: string;
  level: LogLevel;
  message: string;
  module: string;
  details?: Record<string, any>;
}

export interface LogQueryParams {
  startTime?: string;
  endTime?: string;
  level?: LogLevel;
  keyword?: string;
  module?: string;
  page: number;
  pageSize: number;
}

export interface LogQueryResult {
  total: number;
  items: LogEntry[];
} 