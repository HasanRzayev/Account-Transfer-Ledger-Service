import { 
  Account, 
  CreateAccountPayload, 
  TransferPayload, 
  TransferResult, 
  TransferSummary, 
  AccountStatement, 
  ConcurrencyStressTestPayload, 
  ConcurrencyStressTestResult, 
  ApiResponse 
} from './types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080/api';

async function handleResponse<T>(res: Response): Promise<T> {
  const json: ApiResponse<T> = await res.json();
  if (!res.ok || !json.success) {
    const errorMsg = json.error?.message || json.message || `Xəta baş verdi (Status: ${res.status})`;
    const error = new Error(errorMsg) as any;
    error.code = json.error?.code;
    error.statusCode = res.status;
    error.details = json.error?.details;
    throw error;
  }
  return json.data!;
}

export const api = {
  async getAccounts(): Promise<Account[]> {
    const res = await fetch(`${API_BASE_URL}/accounts`, { cache: 'no-store' });
    return handleResponse<Account[]>(res);
  },

  async getAccountById(id: string): Promise<Account> {
    const res = await fetch(`${API_BASE_URL}/accounts/${id}`, { cache: 'no-store' });
    return handleResponse<Account>(res);
  },

  async createAccount(payload: CreateAccountPayload): Promise<Account> {
    const res = await fetch(`${API_BASE_URL}/accounts`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    return handleResponse<Account>(res);
  },

  async transferFunds(payload: TransferPayload, idempotencyKey?: string): Promise<TransferResult> {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };
    if (idempotencyKey) {
      headers['Idempotency-Key'] = idempotencyKey;
    }

    const res = await fetch(`${API_BASE_URL}/transfers`, {
      method: 'POST',
      headers,
      body: JSON.stringify(payload),
    });
    return handleResponse<TransferResult>(res);
  },

  async getRecentTransfers(limit = 20): Promise<TransferSummary[]> {
    const res = await fetch(`${API_BASE_URL}/transfers?limit=${limit}`, { cache: 'no-store' });
    return handleResponse<TransferSummary[]>(res);
  },

  async getStatement(
    accountId: string, 
    pageNumber = 1, 
    pageSize = 10, 
    fromDate?: string, 
    toDate?: string
  ): Promise<AccountStatement> {
    const params = new URLSearchParams({
      pageNumber: pageNumber.toString(),
      pageSize: pageSize.toString(),
    });
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);

    const res = await fetch(`${API_BASE_URL}/statements/${accountId}?${params.toString()}`, { cache: 'no-store' });
    return handleResponse<AccountStatement>(res);
  },

  async runStressTest(payload: ConcurrencyStressTestPayload): Promise<ConcurrencyStressTestResult> {
    const res = await fetch(`${API_BASE_URL}/stresstest/concurrency`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    return handleResponse<ConcurrencyStressTestResult>(res);
  }
};
