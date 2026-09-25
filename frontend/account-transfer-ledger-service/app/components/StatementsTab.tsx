'use client';

import React, { useState, useEffect } from 'react';
import { 
  FileText, 
  Search, 
  ArrowUpRight, 
  ArrowDownLeft, 
  ChevronLeft, 
  ChevronRight, 
  RefreshCw,
  Wallet,
  Calendar,
  Layers,
  Sparkles
} from 'lucide-react';
import { Account, AccountStatement } from '../lib/types';
import { api } from '../lib/api';

interface StatementsTabProps {
  accounts: Account[];
  selectedAccountId?: string;
}

export const StatementsTab: React.FC<StatementsTabProps> = ({
  accounts,
  selectedAccountId: initialSelectedAccountId,
}) => {
  const [selectedAccountId, setSelectedAccountId] = useState(
    initialSelectedAccountId || (accounts[0]?.id || '')
  );
  const [statement, setStatement] = useState<AccountStatement | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchStatement = async () => {
    if (!selectedAccountId) return;
    try {
      setIsLoading(true);
      setError(null);
      const data = await api.getStatement(
        selectedAccountId,
        pageNumber,
        pageSize,
        fromDate || undefined,
        toDate || undefined
      );
      setStatement(data);
    } catch (err: any) {
      setError(err.message || 'Hesab çıxarışı yüklənərkən xəta baş verdi.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (initialSelectedAccountId) {
      setSelectedAccountId(initialSelectedAccountId);
    }
  }, [initialSelectedAccountId]);

  useEffect(() => {
    if (selectedAccountId) {
      fetchStatement();
    }
  }, [selectedAccountId, pageNumber, pageSize]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setPageNumber(1);
    fetchStatement();
  };

  const selectedAccount = accounts.find(a => a.id === selectedAccountId);

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header Bar */}
      <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-xs flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <h2 className="text-lg font-bold text-slate-800">Hesab Çıxarışı və Baş Kitab (Ledger)</h2>
            <span className="text-xs px-2.5 py-0.5 rounded-full bg-blue-50 text-blue-700 border border-blue-200 font-semibold">
              Dapper Powered
            </span>
          </div>
          <p className="text-xs text-slate-500 mt-1">
            Bütün tranzaksiyaların xrono-loji ardıcıllığı və cari qalıq (running balance) hesablanması
          </p>
        </div>

        {/* Account Selector in Header */}
        <div className="w-full md:w-72">
          <label className="block text-[11px] font-semibold text-slate-500 mb-1">Hesab Seçin:</label>
          <select
            value={selectedAccountId}
            onChange={(e) => {
              setSelectedAccountId(e.target.value);
              setPageNumber(1);
            }}
            className="w-full px-3.5 py-2 rounded-xl border border-slate-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-100 text-sm font-semibold text-slate-800 outline-hidden bg-white"
          >
            {accounts.map((acc) => (
              <option key={acc.id} value={acc.id}>
                {acc.accountHolderName} ({acc.accountNumber})
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Account Info & Filter Bar */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-4">
        {/* Balance Snapshot Card */}
        <div className="bg-gradient-to-br from-blue-600 to-indigo-700 text-white p-5 rounded-2xl shadow-sm flex flex-col justify-between">
          <div>
            <span className="text-xs text-blue-100 font-medium">Seçilmiş Hesabın Qalığı:</span>
            <h3 className="text-2xl font-black tracking-tight mt-1">
              {(statement?.currentBalance ?? selectedAccount?.balance ?? 0).toLocaleString('az-AZ', { minimumFractionDigits: 2 })} AZN
            </h3>
          </div>

          <div className="mt-4 pt-3 border-t border-white/20 flex items-center justify-between text-xs text-blue-100 font-medium">
            <span>{statement?.accountHolderName || selectedAccount?.accountHolderName}</span>
            <span className="font-mono text-[11px]">{statement?.accountNumber || selectedAccount?.accountNumber}</span>
          </div>
        </div>

        {/* Filter Controls (3 cols) */}
        <form onSubmit={handleSearch} className="lg:col-span-3 bg-white p-5 rounded-2xl border border-slate-200 shadow-xs flex flex-wrap items-end gap-3">
          <div className="flex-1 min-w-[140px]">
            <label className="block text-xs font-semibold text-slate-600 mb-1">Başlanğıc Tarixi</label>
            <input
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              className="w-full px-3 py-2 rounded-xl border border-slate-300 text-xs text-slate-800 outline-hidden focus:border-blue-500"
            />
          </div>

          <div className="flex-1 min-w-[140px]">
            <label className="block text-xs font-semibold text-slate-600 mb-1">Bitmə Tarixi</label>
            <input
              type="date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
              className="w-full px-3 py-2 rounded-xl border border-slate-300 text-xs text-slate-800 outline-hidden focus:border-blue-500"
            />
          </div>

          <div className="w-28">
            <label className="block text-xs font-semibold text-slate-600 mb-1">Sətir Sayı</label>
            <select
              value={pageSize}
              onChange={(e) => {
                setPageSize(Number(e.target.value));
                setPageNumber(1);
              }}
              className="w-full px-3 py-2 rounded-xl border border-slate-300 text-xs text-slate-800 outline-hidden bg-white focus:border-blue-500"
            >
              <option value={5}>5 sətir</option>
              <option value={10}>10 sətir</option>
              <option value={20}>20 sətir</option>
            </select>
          </div>

          <button
            type="submit"
            className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold shadow-xs transition-colors"
          >
            <Search className="w-3.5 h-3.5" />
            <span>Filtrlə</span>
          </button>
        </form>
      </div>

      {/* Ledger Table */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
        {isLoading ? (
          <div className="py-16 text-center text-slate-500 flex flex-col items-center gap-3">
            <RefreshCw className="w-6 h-6 animate-spin text-blue-600" />
            <span className="text-xs font-semibold">Dapper ilə Baş Kitab qeydləri gətirilir...</span>
          </div>
        ) : error ? (
          <div className="p-8 text-center text-red-600 text-xs font-semibold">
            {error}
          </div>
        ) : !statement || statement.entries.length === 0 ? (
          <div className="py-16 text-center text-slate-400 space-y-2">
            <FileText className="w-8 h-8 mx-auto text-slate-300" />
            <p className="text-xs font-medium">Bu hesab üzrə heç bir əməliyyat qeydi tapılmadı.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-slate-50 border-b border-slate-200 text-[11px] font-bold text-slate-500 uppercase tracking-wider">
                  <th className="py-3.5 px-4">Tarix & Saat</th>
                  <th className="py-3.5 px-4">Növ</th>
                  <th className="py-3.5 px-4">Əməliyyatın Təyinatı & Qarşı Tərəf</th>
                  <th className="py-3.5 px-4 text-right">Məbləğ</th>
                  <th className="py-3.5 px-4 text-right">Cari Qalıq (Running Balance)</th>
                  <th className="py-3.5 px-4 text-center">Tranzaksiya ID</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-xs text-slate-700">
                {statement.entries.map((entry) => {
                  const isCredit = entry.amount > 0;
                  return (
                    <tr key={entry.id} className="hover:bg-slate-50/70 transition-colors">
                      {/* Date */}
                      <td className="py-3.5 px-4 font-mono text-[11px] text-slate-600 whitespace-nowrap">
                        {new Date(entry.createdAtUtc).toLocaleDateString('az-AZ')} {new Date(entry.createdAtUtc).toLocaleTimeString('az-AZ', { hour: '2-digit', minute: '2-digit' })}
                      </td>

                      {/* Entry Type Badge */}
                      <td className="py-3.5 px-4 whitespace-nowrap">
                        {isCredit ? (
                          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200 font-semibold text-[11px]">
                            <ArrowDownLeft className="w-3 h-3" />
                            <span>Mədaxil (Kredit)</span>
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-rose-50 text-rose-700 border border-rose-200 font-semibold text-[11px]">
                            <ArrowUpRight className="w-3 h-3" />
                            <span>Məxaric (Debet)</span>
                          </span>
                        )}
                      </td>

                      {/* Description & Counterparty */}
                      <td className="py-3.5 px-4">
                        <div className="font-semibold text-slate-800">{entry.description}</div>
                        {entry.counterpartyHolderName && (
                          <div className="text-[11px] text-slate-500 mt-0.5">
                            Qarşı tərəf: <strong className="text-slate-700">{entry.counterpartyHolderName}</strong> ({entry.counterpartyAccountNumber})
                          </div>
                        )}
                      </td>

                      {/* Amount */}
                      <td className={`py-3.5 px-4 text-right font-bold font-mono whitespace-nowrap ${
                        isCredit ? 'text-emerald-600' : 'text-rose-600'
                      }`}>
                        {isCredit ? '+' : ''}{entry.amount.toLocaleString('az-AZ', { minimumFractionDigits: 2 })} AZN
                      </td>

                      {/* Running Balance */}
                      <td className="py-3.5 px-4 text-right font-bold font-mono text-slate-800 whitespace-nowrap">
                        {entry.runningBalance.toLocaleString('az-AZ', { minimumFractionDigits: 2 })} AZN
                      </td>

                      {/* Transfer / Entry ID */}
                      <td className="py-3.5 px-4 text-center font-mono text-[10px] text-slate-400 whitespace-nowrap">
                        {entry.transferId ? entry.transferId.substring(0, 8) : 'DEPOSIT'}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination Footer */}
        {statement && statement.totalPages > 0 && (
          <div className="p-4 bg-slate-50 border-t border-slate-200 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-slate-600">
            <div>
              Ümumi <strong>{statement.totalCount}</strong> qeyd | Səhifə <strong>{statement.pageNumber}</strong> / {statement.totalPages}
            </div>

            <div className="flex items-center gap-2">
              <button
                disabled={!statement.hasPreviousPage || isLoading}
                onClick={() => setPageNumber(prev => Math.max(1, prev - 1))}
                className="flex items-center gap-1 px-3 py-1.5 rounded-lg bg-white border border-slate-300 text-slate-700 hover:bg-slate-100 disabled:opacity-40 transition-colors font-semibold"
              >
                <ChevronLeft className="w-3.5 h-3.5" />
                <span>Əvvəlki</span>
              </button>

              <button
                disabled={!statement.hasNextPage || isLoading}
                onClick={() => setPageNumber(prev => prev + 1)}
                className="flex items-center gap-1 px-3 py-1.5 rounded-lg bg-white border border-slate-300 text-slate-700 hover:bg-slate-100 disabled:opacity-40 transition-colors font-semibold"
              >
                <span>Növbəti</span>
                <ChevronRight className="w-3.5 h-3.5" />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
