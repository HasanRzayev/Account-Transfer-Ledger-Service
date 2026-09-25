'use client';

import React, { useState, useEffect, useCallback } from 'react';
import { Navbar } from './components/Navbar';
import { DashboardOverview } from './components/DashboardOverview';
import { AccountsTab } from './components/AccountsTab';
import { TransferTab } from './components/TransferTab';
import { StatementsTab } from './components/StatementsTab';
import { ConcurrencyLabTab } from './components/ConcurrencyLabTab';
import { CreateAccountModal } from './components/CreateAccountModal';
import { Account, TransferSummary, CreateAccountPayload } from './lib/types';
import { api } from './lib/api';
import { AlertCircle, RefreshCw } from 'lucide-react';

export default function Home() {
  const [activeTab, setActiveTab] = useState('dashboard');
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [transfers, setTransfers] = useState<TransferSummary[]>([]);
  
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Cross-tab interaction states
  const [selectedStatementAccountId, setSelectedStatementAccountId] = useState<string | undefined>();
  const [initialTransferAccountId, setInitialTransferAccountId] = useState<string | undefined>();

  const loadData = useCallback(async () => {
    try {
      setIsRefreshing(true);
      setError(null);
      const [accs, txs] = await Promise.all([
        api.getAccounts(),
        api.getRecentTransfers(20),
      ]);
      setAccounts(accs);
      setTransfers(txs);
    } catch (err: any) {
      console.error(err);
      setError(err.message || 'API server ilə əlaqə qurularkən xəta baş verdi. Zəhmət olmasa backend serverin aktiv olduğunu yoxlayın.');
    } finally {
      setIsRefreshing(false);
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleCreateAccount = async (payload: CreateAccountPayload) => {
    await api.createAccount(payload);
    await loadData();
  };

  const handleSelectAccountForStatement = (accountId: string) => {
    setSelectedStatementAccountId(accountId);
    setActiveTab('statements');
  };

  const handleInitiateTransfer = (fromAccountId?: string) => {
    setInitialTransferAccountId(fromAccountId);
    setActiveTab('transfers');
  };

  const totalBalance = accounts.reduce((acc, a) => acc + (a.balance || 0), 0);

  return (
    <div className="min-h-screen bg-[#F8FAFC] text-slate-800 flex flex-col selection:bg-blue-100 selection:text-blue-900">
      {/* Top Navigation */}
      <Navbar
        activeTab={activeTab}
        setActiveTab={setActiveTab}
        onOpenCreateModal={() => setIsCreateModalOpen(true)}
        onRefresh={loadData}
        isRefreshing={isRefreshing}
        totalAccounts={accounts.length}
        totalBalance={totalBalance}
      />

      {/* Main Container */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 flex-1 w-full">
        {error && (
          <div className="mb-6 p-4 rounded-2xl bg-red-50 border border-red-200 text-red-700 text-xs flex items-center justify-between shadow-xs">
            <div className="flex items-center gap-2.5">
              <AlertCircle className="w-5 h-5 shrink-0 text-red-500" />
              <div>
                <strong className="font-bold">Server Xətası:</strong> {error}
              </div>
            </div>
            <button
              onClick={loadData}
              className="px-3 py-1.5 rounded-lg bg-red-100 hover:bg-red-200 text-red-800 text-xs font-semibold transition-colors flex items-center gap-1.5"
            >
              <RefreshCw className="w-3.5 h-3.5" />
              <span>Yenidən Cəhd Et</span>
            </button>
          </div>
        )}

        {/* Tab Switcher */}
        {activeTab === 'dashboard' && (
          <DashboardOverview
            accounts={accounts}
            transfers={transfers}
            onSelectAccountForStatement={handleSelectAccountForStatement}
            onInitiateTransfer={handleInitiateTransfer}
          />
        )}

        {activeTab === 'accounts' && (
          <AccountsTab
            accounts={accounts}
            onOpenCreateModal={() => setIsCreateModalOpen(true)}
            onInitiateTransfer={handleInitiateTransfer}
            onSelectAccountForStatement={handleSelectAccountForStatement}
          />
        )}

        {activeTab === 'transfers' && (
          <TransferTab
            accounts={accounts}
            initialFromAccountId={initialTransferAccountId}
            onTransferSuccess={loadData}
            onExecuteTransfer={api.transferFunds}
          />
        )}

        {activeTab === 'statements' && (
          <StatementsTab
            accounts={accounts}
            selectedAccountId={selectedStatementAccountId}
          />
        )}

        {activeTab === 'concurrency' && (
          <ConcurrencyLabTab
            accounts={accounts}
            onRefreshAccounts={loadData}
          />
        )}
      </main>

      {/* Footer */}
      <footer className="bg-white border-t border-slate-200 py-6 text-center text-xs text-slate-500 mt-auto">
        <div className="max-w-7xl mx-auto px-4 flex flex-col sm:flex-row items-center justify-between gap-2">
          <p>© {new Date().getFullYear()} LedgerBank Xidməti. Double-Entry Architecture & Idempotent Transactions.</p>
          <div className="flex items-center gap-4 text-slate-400">
            <span>ASP.NET Core 8 Web API</span>
            <span>•</span>
            <span>EF Core + Dapper</span>
            <span>•</span>
            <span>Next.js UI</span>
          </div>
        </div>
      </footer>

      {/* Create Account Modal */}
      <CreateAccountModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onSubmit={handleCreateAccount}
      />
    </div>
  );
}
