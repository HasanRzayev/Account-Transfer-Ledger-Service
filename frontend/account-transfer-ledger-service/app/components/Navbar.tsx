'use client';

import React from 'react';
import { 
  Building2, 
  LayoutDashboard, 
  Wallet, 
  ArrowLeftRight, 
  FileText, 
  Zap, 
  ShieldCheck, 
  PlusCircle,
  RefreshCw
} from 'lucide-react';

interface NavbarProps {
  activeTab: string;
  setActiveTab: (tab: string) => void;
  onOpenCreateModal: () => void;
  onRefresh: () => void;
  isRefreshing: boolean;
  totalAccounts: number;
  totalBalance: number;
}

export const Navbar: React.FC<NavbarProps> = ({
  activeTab,
  setActiveTab,
  onOpenCreateModal,
  onRefresh,
  isRefreshing,
  totalAccounts,
  totalBalance,
}) => {
  const tabs = [
    { id: 'dashboard', label: 'İdarəetmə Paneli', icon: LayoutDashboard },
    { id: 'accounts', label: 'Hesablar', icon: Wallet },
    { id: 'transfers', label: 'Pul Köçürməsi', icon: ArrowLeftRight },
    { id: 'statements', label: 'Hesab Çıxarışı (Ledger)', icon: FileText },
    { id: 'concurrency', label: 'Konkurentlik Laboratoriyası', icon: Zap },
  ];

  return (
    <header className="bg-white border-b border-slate-200 sticky top-0 z-30 shadow-xs">
      {/* Top Banner with Quick Telemetry */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Logo & Brand */}
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-blue-600 flex items-center justify-center text-white shadow-sm shadow-blue-200">
              <Building2 className="w-5 h-5" />
            </div>
            <div>
              <span className="font-bold text-lg text-slate-800 tracking-tight">LedgerBank</span>
              <p className="text-xs text-slate-500 font-medium">Hesab Köçürmələri və Baş Kitab Xidməti</p>
            </div>
          </div>

          {/* Center Summary Pills */}
          <div className="hidden md:flex items-center gap-4 text-xs font-medium">
            <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-blue-50 border border-blue-200 text-blue-800">
              <span>Ümumi Depozit:</span>
              <strong className="font-bold text-sm text-blue-900">{totalBalance.toLocaleString('az-AZ', { minimumFractionDigits: 2 })} AZN</strong>
            </div>
          </div>

          {/* Action Buttons */}
          <div className="flex items-center gap-2">
            <button
              onClick={onRefresh}
              disabled={isRefreshing}
              title="Məlumatları Yenilə"
              className="p-2 rounded-lg text-slate-600 hover:text-blue-600 hover:bg-slate-100 transition-colors border border-slate-200 disabled:opacity-50"
            >
              <RefreshCw className={`w-4 h-4 ${isRefreshing ? 'animate-spin text-blue-600' : ''}`} />
            </button>

            <button
              onClick={onOpenCreateModal}
              className="flex items-center gap-1.5 px-3.5 py-2 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold shadow-sm shadow-blue-200 transition-all hover:shadow"
            >
              <PlusCircle className="w-4 h-4" />
              <span>Yeni Hesab Aç</span>
            </button>
          </div>
        </div>

        {/* Tab Navigation */}
        <nav className="flex space-x-1 sm:space-x-4 border-t border-slate-100 overflow-x-auto py-1">
          {tabs.map((tab) => {
            const Icon = tab.icon;
            const isActive = activeTab === tab.id;
            return (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`flex items-center gap-2 px-3.5 py-2.5 rounded-lg text-xs font-semibold transition-all whitespace-nowrap ${
                  isActive
                    ? 'bg-blue-50 text-blue-700 shadow-xs border border-blue-200'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-50'
                }`}
              >
                <Icon className={`w-4 h-4 ${isActive ? 'text-blue-600' : 'text-slate-400'}`} />
                <span>{tab.label}</span>
              </button>
            );
          })}
        </nav>
      </div>
    </header>
  );
};
