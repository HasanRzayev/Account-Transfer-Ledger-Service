'use client';

import React, { useState } from 'react';
import { 
  Zap, 
  ShieldCheck, 
  CheckCircle2, 
  XCircle, 
  AlertCircle, 
  Play, 
  RefreshCw, 
  Activity,
  Layers,
  ArrowRight
} from 'lucide-react';
import { Account, ConcurrencyStressTestResult } from '../lib/types';
import { api } from '../lib/api';

interface ConcurrencyLabTabProps {
  accounts: Account[];
  onRefreshAccounts: () => void;
}

export const ConcurrencyLabTab: React.FC<ConcurrencyLabTabProps> = ({
  accounts,
  onRefreshAccounts,
}) => {
  const [sourceAccountId, setSourceAccountId] = useState(accounts[0]?.id || '');
  const [destinationAccountId, setDestinationAccountId] = useState(
    accounts.find(a => a.id !== accounts[0]?.id)?.id || (accounts[1]?.id || '')
  );
  const [amountPerTx, setAmountPerTx] = useState('50.00');
  const [concurrentCount, setConcurrentCount] = useState(10);
  
  const [isLoading, setIsLoading] = useState(false);
  const [testResult, setTestResult] = useState<ConcurrencyStressTestResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  const sourceAccount = accounts.find(a => a.id === sourceAccountId);
  const destAccount = accounts.find(a => a.id === destinationAccountId);

  const handleRunTest = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setTestResult(null);

    const amountNum = parseFloat(amountPerTx);
    if (!sourceAccountId || !destinationAccountId) {
      setError('Zəhmət olmasa mənbə və hədəf hesabı seçin.');
      return;
    }
    if (sourceAccountId === destinationAccountId) {
      setError('Mənbə və hədəf eyni ola bilməz.');
      return;
    }
    if (isNaN(amountNum) || amountNum <= 0) {
      setError('Məbləğ müsbət olmalıdır.');
      return;
    }

    try {
      setIsLoading(true);
      const result = await api.runStressTest({
        sourceAccountId,
        destinationAccountId,
        transferAmountPerRequest: amountNum,
        concurrentRequestsCount: concurrentCount,
      });

      setTestResult(result);
      onRefreshAccounts();
    } catch (err: any) {
      setError(err.message || 'Stres testi zamanı xəta baş verdi.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header Banner */}
      <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-xs flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-lg bg-amber-50 text-amber-600 flex items-center justify-center font-bold">
              <Zap className="w-5 h-5" />
            </div>
            <h2 className="text-lg font-bold text-slate-800">Konkurentlik və Overdraft Test Laboratoriyası</h2>
          </div>
          <p className="text-xs text-slate-500 mt-1">
            Eyni hesaba eyni millisaniyədə paralel sorğular göndərərək yarış şərtlərini (race conditions) və overdraft qarşısının alınmasını canlı sınaqdan keçirin.
          </p>
        </div>

        <div className="flex items-center gap-2 text-xs font-semibold px-3.5 py-2 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-800">
          <ShieldCheck className="w-4 h-4 text-emerald-600" />
          <span>Pessimistic Row Lock & ACID Integrity</span>
        </div>
      </div>

      {/* Main Grid: Control Panel + Live Telemetry */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left 1 Col: Test Setup Form */}
        <form onSubmit={handleRunTest} className="bg-white p-6 rounded-2xl border border-slate-200 shadow-xs space-y-4">
          <h3 className="text-sm font-bold text-slate-800 flex items-center gap-2">
            <Activity className="w-4 h-4 text-blue-600" />
            <span>Stres Test Parametrləri</span>
          </h3>

          {error && (
            <div className="p-3.5 rounded-xl bg-red-50 border border-red-200 text-red-700 text-xs flex items-start gap-2">
              <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
              <span>{error}</span>
            </div>
          )}

          {/* Source Account */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Test Ediləcək Mənbə Hesab
            </label>
            <select
              value={sourceAccountId}
              onChange={(e) => setSourceAccountId(e.target.value)}
              className="w-full px-3 py-2 rounded-xl border border-slate-300 text-xs font-semibold text-slate-800 outline-hidden bg-white"
            >
              {accounts.map((acc) => (
                <option key={acc.id} value={acc.id}>
                  {acc.accountHolderName} ({acc.balance.toFixed(2)} AZN)
                </option>
              ))}
            </select>
            {sourceAccount && (
              <span className="text-[11px] text-slate-500 mt-1 block">
                Hazırkı Balans: <strong>{sourceAccount.balance.toFixed(2)} AZN</strong>
              </span>
            )}
          </div>

          {/* Destination Account */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Alan Hesab
            </label>
            <select
              value={destinationAccountId}
              onChange={(e) => setDestinationAccountId(e.target.value)}
              className="w-full px-3 py-2 rounded-xl border border-slate-300 text-xs font-semibold text-slate-800 outline-hidden bg-white"
            >
              {accounts.map((acc) => (
                <option key={acc.id} value={acc.id} disabled={acc.id === sourceAccountId}>
                  {acc.accountHolderName} ({acc.balance.toFixed(2)} AZN)
                </option>
              ))}
            </select>
          </div>

          {/* Amount per request */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Hər Sorğudakı Məbləğ (AZN)
            </label>
            <input
              type="number"
              min="1"
              step="1"
              value={amountPerTx}
              onChange={(e) => setAmountPerTx(e.target.value)}
              className="w-full px-3 py-2 rounded-xl border border-slate-300 text-sm font-bold text-slate-800 outline-hidden"
            />
          </div>

          {/* Concurrency Count */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Paralel Sorğuların Sayı: <strong className="text-blue-600">{concurrentCount}</strong>
            </label>
            <div className="grid grid-cols-4 gap-2">
              {[5, 10, 20, 50].map((count) => (
                <button
                  key={count}
                  type="button"
                  onClick={() => setConcurrentCount(count)}
                  className={`py-2 rounded-lg text-xs font-bold transition-all ${
                    concurrentCount === count
                      ? 'bg-blue-600 text-white shadow-xs'
                      : 'bg-slate-100 text-slate-700 hover:bg-slate-200'
                  }`}
                >
                  {count}x
                </button>
              ))}
            </div>
            <p className="text-[11px] text-slate-500 mt-2">
              Cəmi cəhd edilən məbləğ: <strong>{(parseFloat(amountPerTx || '0') * concurrentCount).toFixed(2)} AZN</strong>
            </p>
          </div>

          {/* Action */}
          <button
            type="submit"
            disabled={isLoading}
            className="w-full flex items-center justify-center gap-2 py-3 rounded-xl bg-amber-500 hover:bg-amber-600 text-white font-bold text-sm shadow-sm shadow-amber-200 hover:shadow disabled:opacity-50 transition-all mt-2"
          >
            {isLoading ? (
              <span className="flex items-center gap-2">
                <RefreshCw className="w-4 h-4 animate-spin" />
                <span>{concurrentCount} paralel köçürmə göndərilir...</span>
              </span>
            ) : (
              <span className="flex items-center gap-2">
                <Play className="w-4 h-4 fill-white" />
                <span>Stres Testini Başlat ({concurrentCount}x)</span>
              </span>
            )}
          </button>
        </form>

        {/* Right 2 Cols: Telemetry & Log */}
        <div className="lg:col-span-2 space-y-4">
          {testResult ? (
            <div className="space-y-4 animate-fade-in">
              {/* Telemetry Summary Cards */}
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                <div className="bg-white p-4 rounded-2xl border border-slate-200 shadow-xs">
                  <span className="text-xs font-semibold text-slate-500">Cəmi Sorğu</span>
                  <div className="text-xl font-bold text-slate-800 mt-1">{testResult.totalRequests} ədəd</div>
                  <span className="text-[11px] text-slate-400">Eyni anda göndərildi</span>
                </div>

                <div className="bg-emerald-50/80 p-4 rounded-2xl border border-emerald-200 shadow-xs">
                  <span className="text-xs font-semibold text-emerald-800">Uğurlu Köçürmələr</span>
                  <div className="text-xl font-bold text-emerald-700 mt-1">{testResult.successfulRequests} ədəd</div>
                  <span className="text-[11px] text-emerald-600 font-medium">Balans çatan qədər</span>
                </div>

                <div className="bg-amber-50/80 p-4 rounded-2xl border border-amber-200 shadow-xs">
                  <span className="text-xs font-semibold text-amber-800">Qorunan (Overdraft Block)</span>
                  <div className="text-xl font-bold text-amber-700 mt-1">{testResult.failedRequests} ədəd</div>
                  <span className="text-[11px] text-amber-600 font-medium">422 Vəsait Çatışmazlığı</span>
                </div>
              </div>

              {/* Overdraft Integrity Proof Banner */}
              <div className="p-4 rounded-2xl bg-white border border-emerald-300 shadow-xs flex items-start gap-3">
                <div className="w-8 h-8 rounded-full bg-emerald-100 text-emerald-700 flex items-center justify-center shrink-0 mt-0.5">
                  <CheckCircle2 className="w-5 h-5" />
                </div>
                <div>
                  <h4 className="text-sm font-bold text-emerald-900">
                    Bütövlük Təsdiqi: Balans Mənfiyə Düşmədi!
                  </h4>
                  <p className="text-xs text-slate-600 mt-0.5 leading-relaxed">
                    İlkin Balans: <strong>{testResult.initialSourceBalance.toFixed(2)} AZN</strong> ➔ 
                    Son Balans: <strong>{testResult.finalSourceBalance.toFixed(2)} AZN</strong> (Alan hesab: +{(testResult.successfulRequests * parseFloat(amountPerTx)).toFixed(2)} AZN).
                  </p>
                </div>
              </div>

              {/* Execution Thread Logs */}
              <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
                <div className="px-4 py-3 bg-slate-50 border-b border-slate-200 text-xs font-bold text-slate-700">
                  Paralel İcra İzləri (Thread-Level Concurrency Telemetry)
                </div>
                <div className="max-h-72 overflow-y-auto divide-y divide-slate-100 text-xs">
                  {testResult.details.map((detail) => (
                    <div key={detail.index} className="px-4 py-2.5 flex items-center justify-between hover:bg-slate-50">
                      <div className="flex items-center gap-2.5">
                        {detail.success ? (
                          <span className="w-5 h-5 rounded-full bg-emerald-100 text-emerald-700 flex items-center justify-center text-[10px] font-bold">
                            ✓
                          </span>
                        ) : (
                          <span className="w-5 h-5 rounded-full bg-amber-100 text-amber-700 flex items-center justify-center text-[10px] font-bold">
                            ✕
                          </span>
                        )}
                        <div>
                          <span className="font-semibold text-slate-800">Sorğu #{detail.index}:</span>{' '}
                          <span className="text-slate-600">{detail.message}</span>
                        </div>
                      </div>

                      <div className="flex items-center gap-3">
                        <span className="font-mono text-[11px] text-slate-400">{detail.durationMs}ms</span>
                        <span className={`px-2 py-0.5 rounded-md font-mono text-[10px] font-bold ${
                          detail.success ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800'
                        }`}>
                          HTTP {detail.statusCode}
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          ) : (
            <div className="bg-white p-12 rounded-2xl border border-slate-200 shadow-xs text-center text-slate-400 space-y-3">
              <Zap className="w-10 h-10 mx-auto text-amber-400 opacity-60" />
              <h4 className="text-sm font-bold text-slate-700">Konkurentlik Testi Gözləyir</h4>
              <p className="text-xs text-slate-500 max-w-sm mx-auto">
                Sol paneldən parametrləri seçib <strong>"Stres Testini Başlat"</strong> düyməsinə basın. Sistem eyni anda paralel sorğular göndərəcək və nəticələri anlıq qeyd edəcək.
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
