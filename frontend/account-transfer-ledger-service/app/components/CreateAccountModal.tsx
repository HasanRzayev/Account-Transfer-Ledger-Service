'use client';

import React, { useState } from 'react';
import { X, UserPlus, Sparkles, Check, AlertCircle } from 'lucide-react';
import { CreateAccountPayload } from '../lib/types';

interface CreateAccountModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (payload: CreateAccountPayload) => Promise<void>;
}

export const CreateAccountModal: React.FC<CreateAccountModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
}) => {
  const [name, setName] = useState('');
  const [initialBalance, setInitialBalance] = useState('500.00');
  const [currency, setCurrency] = useState('AZN');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    const balanceNum = parseFloat(initialBalance);
    if (!name.trim()) {
      setError('Hesab sahibinin adı mütləq daxil edilməlidir.');
      return;
    }
    if (isNaN(balanceNum) || balanceNum < 0) {
      setError('İlkin balans 0 və ya daha böyük ədəd olmalıdır.');
      return;
    }

    try {
      setIsLoading(true);
      await onSubmit({
        accountHolderName: name.trim(),
        initialBalance: balanceNum,
        currency: currency.toUpperCase(),
      });
      setName('');
      setInitialBalance('500.00');
      onClose();
    } catch (err: any) {
      setError(err.message || 'Hesab yaradılarkən xəta baş verdi.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/30 backdrop-blur-xs animate-fade-in">
      <div className="bg-white rounded-2xl max-w-md w-full border border-slate-200 shadow-xl overflow-hidden">
        {/* Header */}
        <div className="px-6 py-5 bg-gradient-to-r from-blue-50 to-indigo-50 border-b border-slate-200 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-blue-600 text-white flex items-center justify-center shadow-xs">
              <UserPlus className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-800">Yeni Bank Hesabı Aç</h3>
              <p className="text-xs text-slate-500">İkiqat mühasibatlıq sistemi ilə dərhal aktivləşir</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-white/80 transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          {error && (
            <div className="p-3.5 rounded-xl bg-red-50 border border-red-200 text-red-700 text-xs flex items-start gap-2">
              <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
              <span>{error}</span>
            </div>
          )}

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Hesab Sahibinin Adı və Soyadı *
            </label>
            <input
              type="text"
              required
              placeholder="Məs: Fərid Quliyev"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="w-full px-3.5 py-2.5 rounded-xl border border-slate-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-100 text-sm text-slate-800 placeholder:text-slate-400 outline-hidden transition-all"
            />
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div className="col-span-2">
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                İlkin Açılış Balansı
              </label>
              <div className="relative">
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  placeholder="0.00"
                  value={initialBalance}
                  onChange={(e) => setInitialBalance(e.target.value)}
                  className="w-full pl-3.5 pr-12 py-2.5 rounded-xl border border-slate-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-100 text-sm text-slate-800 outline-hidden font-semibold transition-all"
                />
                <span className="absolute right-3.5 top-1/2 -translate-y-1/2 text-xs font-bold text-slate-400">
                  {currency}
                </span>
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Valyuta
              </label>
              <select
                value={currency}
                onChange={(e) => setCurrency(e.target.value)}
                className="w-full px-3 py-2.5 rounded-xl border border-slate-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-100 text-sm text-slate-800 outline-hidden font-semibold transition-all bg-white"
              >
                <option value="AZN">AZN (₼)</option>
                <option value="USD">USD ($)</option>
                <option value="EUR">EUR (€)</option>
              </select>
            </div>
          </div>

          {/* Ledger Notice */}
          <div className="p-3 rounded-xl bg-blue-50/60 border border-blue-100 text-xs text-blue-800 space-y-1">
            <div className="flex items-center gap-1.5 font-semibold text-blue-900">
              <Sparkles className="w-3.5 h-3.5 text-blue-600" />
              <span>Baş Kitab (Double-Entry Ledger) Zəmanəti:</span>
            </div>
            <p className="text-slate-600 leading-relaxed text-[11px]">
              Daxil edilən ilkin balans cədvəldə sabit rəqəm kimi deyil, birbaşa Baş Kitabda (Ledger) atomik <strong>Kredit (+)</strong> qeydi kimi formalaşdırılır.
            </p>
          </div>

          {/* Action Buttons */}
          <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-100">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2.5 rounded-xl text-xs font-semibold text-slate-600 hover:bg-slate-100 transition-colors"
            >
              Ləğv et
            </button>
            <button
              type="submit"
              disabled={isLoading}
              className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold shadow-sm shadow-blue-200 hover:shadow disabled:opacity-50 transition-all"
            >
              {isLoading ? (
                <span>Hesab açılır...</span>
              ) : (
                <>
                  <Check className="w-4 h-4" />
                  <span>Hesabı Təsdiqlə və Aç</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
