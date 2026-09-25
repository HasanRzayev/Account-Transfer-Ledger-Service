export interface Account {
  id: string;
  accountNumber: string;
  accountHolderName: string;
  currency: string;
  balance: number;
  isActive: boolean;
  createdAtUtc: string;
  totalTransactionsCount: number;
}

export interface CreateAccountPayload {
  accountHolderName: string;
  initialBalance: number;
  currency: string;
}

export interface TransferPayload {
  fromAccountId: string;
  toAccountId: string;
  amount: number;
  description: string;
}

export interface TransferResult {
  transferId: string;
  fromAccountId: string;
  fromAccountNumber: string;
  fromAccountHolder: string;
  toAccountId: string;
  toAccountNumber: string;
  toAccountHolder: string;
  amount: number;
  currency: string;
  description: string;
  idempotencyKey: string;
  sourceNewBalance: number;
  destinationNewBalance: number;
  createdAtUtc: string;
  wasCachedResponse: boolean;
}

export interface TransferSummary {
  id: string;
  fromAccountId: string;
  fromAccountNumber: string;
  fromAccountHolder: string;
  toAccountId: string;
  toAccountNumber: string;
  toAccountHolder: string;
  amount: number;
  currency: string;
  description: string;
  idempotencyKey: string;
  createdAtUtc: string;
}

export interface LedgerEntry {
  id: string;
  accountId: string;
  transferId?: string | null;
  amount: number;
  entryType: "Debit" | "Credit" | number;
  entryTypeName: string;
  description: string;
  runningBalance: number;
  createdAtUtc: string;
  counterpartyAccountNumber?: string | null;
  counterpartyHolderName?: string | null;
}

export interface AccountStatement {
  accountId: string;
  accountNumber: string;
  accountHolderName: string;
  currency: string;
  currentBalance: number;
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  entries: LedgerEntry[];
}

export interface ConcurrencyStressTestPayload {
  sourceAccountId: string;
  destinationAccountId: string;
  transferAmountPerRequest: number;
  concurrentRequestsCount: number;
}

export interface StressTestDetailItem {
  index: number;
  success: boolean;
  statusCode: number;
  message: string;
  errorCode?: string | null;
  durationMs: number;
}

export interface ConcurrencyStressTestResult {
  totalRequests: number;
  successfulRequests: number;
  failedRequests: number;
  initialSourceBalance: number;
  finalSourceBalance: number;
  finalDestinationBalance: number;
  overdraftPrevented: boolean;
  summaryMessage: string;
  details: StressTestDetailItem[];
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  error?: {
    code: string;
    message: string;
    statusCode: number;
    details?: any;
    timestampUtc: string;
  };
}
