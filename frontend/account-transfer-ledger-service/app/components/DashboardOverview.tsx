'use client';

import React from 'react';
import { 
  Wallet, 
  ArrowUpRight, 
  ArrowDownLeft, 
  Activity, 
  ShieldCheck, 
  Clock, 
  CheckCircle2, 
  Layers, 
  Send,
  FileText,
  Copy,
  ExternalLink
} from 'lucide-react';
import { Account, TransferSummary } from '../lib/types';

interface DashboardOverviewProps {
  accounts: Account[];
  transfers: TransferSummary[];
  onSelectAccountForStatement: (accountId: string) => void;
  onInitiateTransfer: (fromAccountId?: string) => void;
}

export const DashboardOverview: React.FC<DashboardOverviewProps> = ({
  accounts,
  transfers,
  onSelectAccountForStatement,
  onInitiateTransfer,
}) => {
  const totalBalance = accounts.reduce((acc, a) => acc + (a.balance || 0), 0);
  const totalLedgerTxs = accounts.reduce((acc, a) => acc + (a.totalTransactionsCount || 0), 0);

  const [copiedId, setCopiedId] = React.useState<string | null>(null);

  const handleCopy = (text: string, id: string) => {
    navigator.clipboard.writeText(text);
    setCopiedId(id);
    setTimeout(() => setCopiedId(null), 2000);
  };

  return (
    <div className="space-y-6 animate-fade-in">
      {/* 4 Stats Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Total Ledger Balance */}
        <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-xs hover:border-blue-300 transition-all">
          <div className="flex items-center justify-between mb-3">
            <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Ümumi Likvidlik</span>
            <div className="w-9 h-9 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center">
              <Wallet className="w-5 h-5" />
            </div>
          </div>
          <div className="text-2xl font-bold text-slate-800 tracking-tight">
            {totalBalance.toLocaleString('az-AZ', { minimumFractionDigits: 2 })} <span className="text-sm font-semibold text-blue-600">AZN</span>
          </div>
          <div className="mt-2 flex items-center gap-1.5 text-xs text-emerald-600 font-medium">
            <CheckCircle2 className="w-3.5 h-3.5" />
            <span>Baş Kitabdan törəmə (Immutable)</span>
          </div>
        </div>

        {/* Active Accounts */}
        <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-xs hover:border-blue-300 transition-all">
          <div className="flex items-center justify-between mb-3">
            <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Aktiv Hesablar</span>
            <div className="w-9 h-9 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center">
              <Layers className="w-5 h-5" />
            </div>
          </div>
          <div className="text-2xl font-bold text-slate-800 tracking-tight">
            {accounts.length} <span className="text-sm font-normal text-slate-500">hesab</span>
          </div>
          <div className="mt-2 flex items-center gap-1.5 text-xs text-slate-500 font-medium">
            <span className="w-2 h-2 rounded-full bg-emerald-500 inline-block"></span>
            <span>Hamısı tam aktiv statusdadır</span>
          </div>
        </div>

        {/* Total Ledger Transactions */}
        <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-xs hover:border-blue-300 transition-all">
          <div className="flex items-center justify-between mb-3">
            <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Baş Kitab Əməliyyatları</span>
            <div className="w-9 h-9 rounded-xl bg-indigo-50 text-indigo-600 flex items-center justify-center">
              <Activity className="w-5 h-5" />
            </div>
          </div>
          <div className="text-2xl font-bold text-slate-800 tracking-tight">
            {totalLedgerTxs} <span className="text-sm font-normal text-slate-500">qeyd</span>
          </div>
          <div className="mt-2 flex items-center gap-1.5 text-xs text-indigo-600 font-medium">
            <span>Dapper ilə yüksək sürət</span>
          </div>
        </div>

        {/* Security & Concurrency Standard */}
        <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-xs hover:border-blue-300 transition-all">
          <div className="flex items-center justify-between mb-3">
            <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Tranzaksiya Bütövlüyü</span>
            <div className="w-9 h-9 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center">
              <ShieldCheck className="w-5 h-5" />
            </div>
          </div>
          <div className="text-2xl font-bold text-emerald-700 tracking-tight">
            100% ACID
          </div>
          <div className="mt-2 flex items-center gap-1.5 text-xs text-slate-600 font-medium">
            <span>Row-Level Lock + Idempotency</span>
          </div>
        </div>
      </div>

      {/* Two Column Layout: Accounts Quick Cards + Recent Transfers */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left 2 Cols: Accounts List */}
        <div className="lg:col-span-2 space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="text-base font-bold text-slate-800">Bank Hesabları və Balanslar</h3>
              <p className="text-xs text-slate-500">Bütün balanslar birbaşa Baş Kitab (Ledger) qeydlərindən hesablanır</p>
            </div>
            <button
              onClick={() => onInitiateTransfer()}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-blue-50 hover:bg-blue-100 text-blue-700 text-xs font-semibold border border-blue-200 transition-colors"
            >
              <Send className="w-3.5 h-3.5" />
              <span>Köçürmə Et</span>
            </button>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
            {accounts.map((account) => (
              <div
                key={account.id}
                className="bg-white p-4 rounded-2xl border border-slate-200 shadow-xs hover:border-blue-400 hover:shadow-sm transition-all group"
              >
                <div className="flex items-start justify-between">
                  <div>
                    <span className="inline-block px-2 py-0.5 rounded-md bg-slate-100 text-slate-700 font-mono text-[11px] font-semibold">
                      {account.accountNumber}
                    </span>
                    <h4 className="text-sm font-bold text-slate-800 mt-1.5 group-hover:text-blue-600 transition-colors">
                      {account.accountHolderName}
                    </h4>
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

                <div className="mt-3 pt-3 border-t border-slate-100 flex items-center justify-between">
                  <div>
                    <span className="text-[11px] text-slate-500 font-medium block">Törəmə Qalıq:</span>
                    <span className="text-base font-bold text-slate-800">
                      {account.balance.toLocaleString('az-AZ', { minimumFractionDigits: 2 })} {account.currency}
                    </span>
                  </div>

                  <div className="flex items-center gap-1.5">
                    <button
                      onClick={() => onSelectAccountForStatement(account.id)}
                      title="Hesab çıxarışına bax"
                      className="p-2 rounded-lg bg-slate-50 hover:bg-blue-50 text-slate-600 hover:text-blue-600 border border-slate-200 transition-colors"
                    >
                      <FileText className="w-3.5 h-3.5" />
                    </button>
                    <button
                      onClick={() => onInitiateTransfer(account.id)}
                      title="Bu hesabdan köçürmə et"
                      className="p-2 rounded-lg bg-blue-600 hover:bg-blue-700 text-white shadow-xs transition-colors"
                    >
                      <Send className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Right 1 Col: Recent Transfers */}
        <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-xs flex flex-col justify-between">
          <div>
            <div className="flex items-center justify-between pb-3 border-b border-slate-100 mb-4">
              <div className="flex items-center gap-2">
                <Clock className="w-4 h-4 text-slate-500" />
                <h3 className="text-sm font-bold text-slate-800">Son Köçürmələr</h3>
              </div>
              <span className="text-[11px] font-semibold text-slate-500 bg-slate-100 px-2 py-0.5 rounded-full">
                Canlı
              </span>
            </div>

            {transfers.length === 0 ? (
              <div className="py-12 text-center text-slate-400">
                <Activity className="w-8 h-8 mx-auto mb-2 opacity-40 text-slate-400" />
                <p className="text-xs font-medium">Hələlik heç bir köçürmə icra edilməyib.</p>
              </div>
            ) : (
              <div className="space-y-3">
                {transfers.slice(0, 5).map((tx) => (
                  <div
                    key={tx.id}
                    className="p-3 rounded-xl bg-slate-50/80 hover:bg-slate-100/80 border border-slate-100 transition-colors"
                  >
                    <div className="flex items-center justify-between text-xs">
                      <span className="font-semibold text-slate-800">{tx.fromAccountHolder}</span>
                      <span className="font-bold text-emerald-700">
                        {tx.amount.toLocaleString('az-AZ', { minimumFractionDigits: 2 })} {tx.currency}
                      </span>
                    </div>
                    <div className="flex items-center justify-between text-[11px] text-slate-500 mt-1">
                      <span>➔ {tx.toAccountHolder}</span>
                      <span>{new Date(tx.createdAtUtc).toLocaleTimeString('az-AZ', { hour: '2-digit', minute: '2-digit' })}</span>
                    </div>
                    {tx.description && (
                      <p className="text-[10px] text-slate-400 truncate mt-1 italic">
                        "{tx.description}"
                      </p>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>

          <div className="mt-4 pt-3 border-t border-slate-100 text-center">
            <span className="text-[11px] text-slate-400">Bütün əməliyyatlar atomik olaraq 2 Baş Kitab sətrindən ibarətdir</span>
          </div>
        </div>
      </div>
    </div>
  );
};
