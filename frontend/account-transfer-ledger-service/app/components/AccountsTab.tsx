'use client';

import React, { useState } from 'react';
import { 
  Wallet, 
  Search, 
  PlusCircle, 
  Send, 
  FileText, 
  Copy, 
  CheckCircle2, 
  Layers, 
  Calendar,
  Activity
} from 'lucide-react';
import { Account } from '../lib/types';

interface AccountsTabProps {
  accounts: Account[];
  onOpenCreateModal: () => void;
  onInitiateTransfer: (fromAccountId?: string) => void;
  onSelectAccountForStatement: (accountId: string) => void;
}

export const AccountsTab: React.FC<AccountsTabProps> = ({
  accounts,
  onOpenCreateModal,
  onInitiateTransfer,
  onSelectAccountForStatement,
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [copiedId, setCopiedId] = useState<string | null>(null);

  const handleCopy = (text: string, id: string) => {
    navigator.clipboard.writeText(text);
    setCopiedId(id);
    setTimeout(() => setCopiedId(null), 2000);
  };

  const filteredAccounts = accounts.filter(a => 
    a.accountHolderName.toLowerCase().includes(searchTerm.toLowerCase()) ||
    a.accountNumber.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Top Header & Search Bar */}
      <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-xs flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-lg font-bold text-slate-800">Bank Hesablarının İdarə Edilməsi</h2>
          <p className="text-xs text-slate-500 mt-1">
            Bütün hesablar və onların Baş Kitabdan dinamik hesablanmış törəmə balansları
          </p>
        </div>

        <div className="flex items-center gap-3 w-full sm:w-auto">
          <div className="relative flex-1 sm:w-64">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              placeholder="Ad və ya hesab nömrəsi axtar..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-9 pr-3 py-2 rounded-xl border border-slate-300 text-xs text-slate-800 outline-hidden focus:border-blue-500 bg-slate-50 focus:bg-white transition-all"
            />
          </div>

          <button
            onClick={onOpenCreateModal}
            className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold shadow-xs transition-colors whitespace-nowrap"
          >
            <PlusCircle className="w-4 h-4" />
            <span>Yeni Hesab Aç</span>
          </button>
        </div>
      </div>

      {/* Accounts Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {filteredAccounts.map((account) => (
          <div
            key={account.id}
            className="bg-white rounded-2xl border border-slate-200 p-5 shadow-xs hover:border-blue-300 hover:shadow-sm transition-all flex flex-col justify-between"
          >
            <div>
              {/* Account Header */}
              <div className="flex items-start justify-between">
                <div>
                  <div className="flex items-center gap-1.5">
                    <span className="font-mono text-xs font-bold text-blue-700 bg-blue-50 px-2 py-0.5 rounded-md border border-blue-100">
                      {account.accountNumber}
                    </span>
                    <span className="w-2 h-2 rounded-full bg-emerald-500" title="Aktiv"></span>
                  </div>
                  <h3 className="text-base font-bold text-slate-800 mt-2">
                    {account.accountHolderName}
                  </h3>
                </div>

                <button
                  onClick={() => handleCopy(account.accountNumber, account.id)}
                  title="Hesab nömrəsini kopyala"
                  className="p-1.5 rounded-lg text-slate-400 hover:text-blue-600 hover:bg-blue-50 transition-colors"
                >
                  {copiedId === account.id ? (
                    <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                  ) : (
                    <Copy className="w-4 h-4" />
                  )}
                </button>
              </div>

              {/* Dynamic Balance Box */}
              <div className="mt-4 p-3 rounded-xl bg-slate-50 border border-slate-100">
                <span className="text-[11px] font-semibold text-slate-500 uppercase tracking-wider block">
                  Törəmə Balans (SUM of Ledger)
                </span>
                <span className="text-xl font-black text-slate-800 tracking-tight mt-0.5 block">
                  {account.balance.toLocaleString('az-AZ', { minimumFractionDigits: 2 })} <span className="text-xs font-bold text-blue-600">{account.currency}</span>
                </span>
              </div>

              {/* Account Meta */}
              <div className="mt-3 flex items-center justify-between text-[11px] text-slate-500">
                <span className="flex items-center gap-1">
                  <Activity className="w-3.5 h-3.5 text-slate-400" />
                  <span>{account.totalTransactionsCount || 0} əməliyyat</span>
                </span>
                <span className="flex items-center gap-1">
                  <Calendar className="w-3.5 h-3.5 text-slate-400" />
                  <span>{new Date(account.createdAtUtc).toLocaleDateString('az-AZ')}</span>
                </span>
              </div>
            </div>

            {/* Card Actions */}
            <div className="mt-4 pt-3 border-t border-slate-100 flex items-center gap-2">
              <button
                onClick={() => onSelectAccountForStatement(account.id)}
                className="flex-1 flex items-center justify-center gap-1.5 py-2 rounded-xl bg-slate-50 hover:bg-blue-50 text-slate-700 hover:text-blue-700 text-xs font-semibold border border-slate-200 transition-colors"
              >
                <FileText className="w-3.5 h-3.5" />
                <span>Çıxarışa Bax</span>
              </button>

              <button
                onClick={() => onInitiateTransfer(account.id)}
                className="flex-1 flex items-center justify-center gap-1.5 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold shadow-xs transition-colors"
              >
                <Send className="w-3.5 h-3.5" />
                <span>Köçürmə Et</span>
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
