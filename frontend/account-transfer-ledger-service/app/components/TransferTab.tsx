'use client';

import React, { useState, useEffect } from 'react';
import { 
  ArrowLeftRight, 
  ShieldCheck, 
  RefreshCw, 
  CheckCircle2, 
  AlertCircle, 
  Sparkles, 
  Send,
  Key,
  Info,
  Layers,
  ArrowRight
} from 'lucide-react';
import { Account, TransferResult } from '../lib/types';

interface TransferTabProps {
  accounts: Account[];
  initialFromAccountId?: string;
  onTransferSuccess: () => void;
  onExecuteTransfer: (payload: { fromAccountId: string; toAccountId: string; amount: number; description: string }, idempotencyKey: string) => Promise<TransferResult>;
}

export const TransferTab: React.FC<TransferTabProps> = ({
  accounts,
  initialFromAccountId,
  onTransferSuccess,
  onExecuteTransfer,
}) => {
  const [fromAccountId, setFromAccountId] = useState(initialFromAccountId || (accounts[0]?.id || ''));
  const [toAccountId, setToAccountId] = useState(accounts.find(a => a.id !== fromAccountId)?.id || (accounts[1]?.id || ''));
  const [amount, setAmount] = useState('50.00');
  const [description, setDescription] = useState('Hesablararası daxili köçürmə');
  const [idempotencyKey, setIdempotencyKey] = useState(`req-${Date.now()}-${Math.random().toString(36).substring(2, 7)}`);
  
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [lastResult, setLastResult] = useState<TransferResult | null>(null);

  useEffect(() => {
    if (initialFromAccountId) {
      setFromAccountId(initialFromAccountId);
      const other = accounts.find(a => a.id !== initialFromAccountId);
      if (other) setToAccountId(other.id);
    }
  }, [initialFromAccountId, accounts]);

  const fromAccount = accounts.find(a => a.id === fromAccountId);
  const toAccount = accounts.find(a => a.id === toAccountId);

  const generateNewKey = () => {
    setIdempotencyKey(`req-${Date.now()}-${Math.random().toString(36).substring(2, 7)}`);
  };

  const handleTransfer = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    setError(null);

    const amountNum = parseFloat(amount);
    if (!fromAccountId || !toAccountId) {
      setError('Zəhmət olmasa göndərən və alan hesabı seçin.');
      return;
    }
    if (fromAccountId === toAccountId) {
      setError('Göndərən və alan hesab eyni ola bilməz. Fərqli hesab seçin.');
      return;
    }
    if (isNaN(amountNum) || amountNum <= 0) {
      setError('Köçürmə məbləği 0.01 AZN-dən böyük olmalıdır.');
      return;
    }
    if (fromAccount && fromAccount.balance < amountNum) {
      setError(`Hesabda kifayət qədər vəsait yoxdur. Mövcud balans: ${fromAccount.balance.toLocaleString('az-AZ', { minimumFractionDigits: 2 })} AZN.`);
      return;
    }

    try {
      setIsLoading(true);
      const res = await onExecuteTransfer({
        fromAccountId,
        toAccountId,
        amount: amountNum,
        description: description.trim(),
      }, idempotencyKey);

      setLastResult(res);
      onTransferSuccess();
    } catch (err: any) {
      setError(err.message || 'Köçürmə zamanı xəta baş verdi.');
    } finally {
      setIsLoading(false);
    }
  };

  const setPresetAmount = (val: number) => {
    setAmount(val.toFixed(2));
  };

  const setMaxAmount = () => {
    if (fromAccount) {
      setAmount(fromAccount.balance.toFixed(2));
    }
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6 animate-fade-in">
      {/* Header Banner */}
      <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-xs flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <h2 className="text-lg font-bold text-slate-800">Atomik Pul Köçürməsi</h2>
            <span className="text-xs px-2.5 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200 font-semibold">
              ACID Transaction
            </span>
          </div>
          <p className="text-xs text-slate-500 mt-1">
            İkiqat qeydiyyat (Double-Entry) və Idempotency-Key ilə tam təhlükəsiz köçürmə
          </p>
        </div>

        <div className="flex items-center gap-2 text-xs font-semibold px-3.5 py-2 rounded-xl bg-blue-50 border border-blue-200 text-blue-800">
          <ShieldCheck className="w-4 h-4 text-blue-600" />
          <span>Overdraft Qarşısının Alınması: 100% Zəmanət</span>
        </div>
      </div>

      {/* Main Grid: Form + Summary Panel */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left 2 Cols: Form */}
        <form onSubmit={handleTransfer} className="lg:col-span-2 bg-white p-6 rounded-2xl border border-slate-200 shadow-xs space-y-5">
          {error && (
            <div className="p-4 rounded-xl bg-red-50 border border-red-200 text-red-700 text-xs flex items-start gap-2.5">
              <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
              <div>
                <strong className="block font-bold">Köçürmə dayandırıldı:</strong>
                <span>{error}</span>
              </div>
            </div>
          )}

          {/* Account Selectors */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 relative">
            {/* From Account */}
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Göndərən Hesab (Mənbə) *
              </label>
              <select
                value={fromAccountId}
                onChange={(e) => setFromAccountId(e.target.value)}
                className="w-full px-3.5 py-2.5 rounded-xl border border-slate-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-100 text-sm text-slate-800 outline-hidden font-medium bg-white"
              >
                {accounts.map((acc) => (
                  <option key={acc.id} value={acc.id}>
                    {acc.accountHolderName} ({acc.accountNumber}) - {acc.balance.toLocaleString('az-AZ', { minimumFractionDigits: 2 })} AZN
                  </option>
                ))}
              </select>
              {fromAccount && (
                <div className="mt-1.5 flex items-center justify-between text-xs text-slate-500">
                  <span>Mövcud Balans:</span>
                  <span className="font-bold text-slate-800">{fromAccount.balance.toLocaleString('az-AZ', { minimumFractionDigits: 2 })} {fromAccount.currency}</span>
                </div>
              )}
            </div>

            {/* To Account */}
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Alan Hesab (Hədəf) *
              </label>
              <select
                value={toAccountId}
                onChange={(e) => setToAccountId(e.target.value)}
                className="w-full px-3.5 py-2.5 rounded-xl border border-slate-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-100 text-sm text-slate-800 outline-hidden font-medium bg-white"
              >
                {accounts.map((acc) => (
                  <option key={acc.id} value={acc.id} disabled={acc.id === fromAccountId}>
                    {acc.accountHolderName} ({acc.accountNumber}) {acc.id === fromAccountId ? '(Eyni hesab)' : ''}
                  </option>
                ))}
              </select>
              {toAccount && (
                <div className="mt-1.5 flex items-center justify-between text-xs text-slate-500">
                  <span>Cari Balansı:</span>
                  <span className="font-bold text-slate-800">{toAccount.balance.toLocaleString('az-AZ', { minimumFractionDigits: 2 })} {toAccount.currency}</span>
                </div>
              )}
            </div>
          </div>

          {/* Amount and Presets */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Köçürmə Məbləği (AZN) *
            </label>
            <div className="relative">
              <input
                type="number"
                min="0.01"
                step="0.01"
                placeholder="0.00"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                className="w-full pl-4 pr-16 py-3 rounded-xl border border-slate-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-100 text-lg font-bold text-slate-800 outline-hidden transition-all"
              />
              <span className="absolute right-4 top-1/2 -translate-y-1/2 text-sm font-bold text-slate-400">
                AZN
              </span>
            </div>

            {/* Presets */}
            <div className="flex flex-wrap items-center gap-2 mt-2.5">
              <span className="text-[11px] font-semibold text-slate-400 mr-1">Sürətli seçimlər:</span>
              {[20, 50, 100, 250, 500].map((val) => (
                <button
                  key={val}
                  type="button"
                  onClick={() => setPresetAmount(val)}
                  className="px-2.5 py-1 rounded-lg bg-slate-100 hover:bg-blue-50 hover:text-blue-700 text-slate-600 text-xs font-semibold border border-slate-200 transition-colors"
                >
                  +{val} ₼
                </button>
              ))}
              <button
                type="button"
                onClick={setMaxAmount}
                className="px-2.5 py-1 rounded-lg bg-blue-50 hover:bg-blue-100 text-blue-700 text-xs font-semibold border border-blue-200 transition-colors"
              >
                Maksimum
              </button>
            </div>
          </div>

          {/* Description */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Təyinat / Əməliyyat Şərhi
            </label>
            <input
              type="text"
              placeholder="Məs: Şəxsi borcun qaytarılması"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full px-3.5 py-2.5 rounded-xl border border-slate-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-100 text-sm text-slate-800 placeholder:text-slate-400 outline-hidden"
            />
          </div>

          {/* Idempotency Key Control */}
          <div className="p-4 rounded-xl bg-slate-50 border border-slate-200 space-y-2">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-1.5 text-xs font-bold text-slate-700">
                <Key className="w-3.5 h-3.5 text-blue-600" />
                <span>Idempotency-Key (Təkrar İcranın Qarşısını Alan Unikal Açar):</span>
              </div>
              <button
                type="button"
                onClick={generateNewKey}
                className="text-[11px] text-blue-600 hover:text-blue-800 font-semibold flex items-center gap-1"
              >
                <RefreshCw className="w-3 h-3" />
                <span>Yeni Açar Yarat</span>
              </button>
            </div>
            <input
              type="text"
              value={idempotencyKey}
              onChange={(e) => setIdempotencyKey(e.target.value)}
              className="w-full px-3 py-2 rounded-lg bg-white border border-slate-300 font-mono text-xs text-slate-700 outline-hidden"
            />
            <p className="text-[11px] text-slate-500">
              Bu açar eyni qaldıqda, təkrar basıldıqda ikili vəsait silinməsi baş vermir, keşlənmiş əvvəlki cavab qaytarılır.
            </p>
          </div>

          {/* Submit Actions */}
          <div className="flex flex-col sm:flex-row items-center gap-3 pt-2">
            <button
              type="submit"
              disabled={isLoading}
              className="w-full sm:flex-1 flex items-center justify-center gap-2 py-3 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-bold text-sm shadow-sm shadow-blue-200 hover:shadow disabled:opacity-50 transition-all"
            >
              {isLoading ? (
                <span className="flex items-center gap-2">
                  <RefreshCw className="w-4 h-4 animate-spin" />
                  <span>Köçürmə icra olunur...</span>
                </span>
              ) : (
                <span className="flex items-center gap-2">
                  <Send className="w-4 h-4" />
                  <span>Köçürməni İcra Et ({parseFloat(amount || '0').toLocaleString('az-AZ', { minimumFractionDigits: 2 })} AZN)</span>
                </span>
              )}
            </button>

            <button
              type="button"
              disabled={isLoading}
              onClick={() => handleTransfer()}
              title="Eyni açarla yenidən göndər və təkrar icranın qarşısının alındığını gör"
              className="w-full sm:w-auto px-4 py-3 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-semibold border border-slate-200 transition-colors whitespace-nowrap"
            >
              Təkrar Göndər (İdempotent Test)
            </button>
          </div>
        </form>

        {/* Right 1 Col: Transfer Summary & Live Result */}
        <div className="space-y-4">
          {/* Live Preview Card */}
          <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-xs space-y-4">
            <h3 className="text-sm font-bold text-slate-800 flex items-center gap-2">
              <Sparkles className="w-4 h-4 text-blue-600" />
              <span>Əməliyyat Xülasəsi</span>
            </h3>

            <div className="space-y-2.5 text-xs">
              <div className="flex justify-between py-1.5 border-b border-slate-100">
                <span className="text-slate-500">Göndərən:</span>
                <span className="font-semibold text-slate-800 text-right">{fromAccount?.accountHolderName || '-'}</span>
              </div>
              <div className="flex justify-between py-1.5 border-b border-slate-100">
                <span className="text-slate-500">Alan:</span>
                <span className="font-semibold text-slate-800 text-right">{toAccount?.accountHolderName || '-'}</span>
              </div>
              <div className="flex justify-between py-1.5 border-b border-slate-100">
                <span className="text-slate-500">Köçürmə Məbləği:</span>
                <span className="font-bold text-blue-600">{parseFloat(amount || '0').toFixed(2)} AZN</span>
              </div>
              <div className="flex justify-between py-1.5 border-b border-slate-100">
                <span className="text-slate-500">Komissiya:</span>
                <span className="font-bold text-emerald-600">0.00 AZN (Pulsuz)</span>
              </div>
              <div className="flex justify-between py-1.5">
                <span className="text-slate-500">Ledger Qeydləri:</span>
                <span className="font-semibold text-slate-800">1 Debet (-) + 1 Kredit (+)</span>
              </div>
            </div>
          </div>

          {/* Last Result Card */}
          {lastResult && (
            <div className="bg-emerald-50/80 p-5 rounded-2xl border border-emerald-200 shadow-xs space-y-3 animate-fade-in">
              <div className="flex items-center gap-2 text-emerald-800 font-bold text-sm">
                <CheckCircle2 className="w-5 h-5 text-emerald-600 shrink-0" />
                <span>Köçürmə Uğurla Tamamlandı!</span>
              </div>

              {lastResult.wasCachedResponse && (
                <div className="p-2 rounded-lg bg-amber-100/70 border border-amber-300 text-amber-800 text-[11px] font-semibold flex items-center gap-1.5">
                  <Info className="w-3.5 h-3.5 text-amber-700" />
                  <span>İdempotent Cavab: Bu sorğu təkrarlandı və keşdən qaytarıldı (İkili çıxarış edilmədi).</span>
                </div>
              )}

              <div className="space-y-1.5 text-xs text-emerald-900 font-medium pt-1">
                <div className="flex justify-between">
                  <span className="text-emerald-700">Məbləğ:</span>
                  <span className="font-bold">{lastResult.amount.toFixed(2)} {lastResult.currency}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-emerald-700">Göndərənin Yeni Balansı:</span>
                  <span className="font-bold">{lastResult.sourceNewBalance.toFixed(2)} AZN</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-emerald-700">Alanın Yeni Balansı:</span>
                  <span className="font-bold">{lastResult.destinationNewBalance.toFixed(2)} AZN</span>
                </div>
                <div className="flex justify-between text-[10px] text-emerald-600 pt-1">
                  <span>Tranzaksiya ID:</span>
                  <span className="font-mono">{lastResult.transferId.substring(0, 13)}...</span>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
