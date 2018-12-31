#include "pch.h"
#include "Analyzer.h"

using namespace Kernel;
using namespace Platform;

Analyzer::Analyzer()
{
}

std::string WStringToString(const wchar_t* wstr)
{
	int iLen = WideCharToMultiByte(CP_ACP, 0, wstr,
		(int)std::wcslen(wstr), NULL, 0, NULL, NULL);

	if (iLen <= 0)
		return { "" };

	char *szDst = new char[iLen + 1];
	if (szDst == nullptr)
		return { "" };

	WideCharToMultiByte(CP_ACP, 0, wstr,
		(int)std::wcslen(wstr), szDst, iLen, NULL, NULL);
	szDst[iLen] = '\0';

	std::string str{ szDst };
	delete[] szDst;

	return str;
}

std::wstring StringToWString(const char* str)
{
	int iLen = MultiByteToWideChar(CP_ACP, 0, str,
		(int)std::strlen(str), NULL, 0);

	if (iLen <= 0)
		return { L"" };

	wchar_t *wszDst = new wchar_t[iLen + 1];
	if (wszDst == nullptr)
		return { L"" };

	MultiByteToWideChar(CP_ACP, 0, str,
		(int)std::strlen(str), wszDst, iLen);
	wszDst[iLen] = L'\0';

	if (wszDst[0] == 0xFEFF)
		for (int i = 0; i < iLen; ++i)
			wszDst[i] = wszDst[i + 1];

	std::wstring wstr{ wszDst };
	delete wszDst;
	wszDst = nullptr;

	return wstr;
}

void Kernel::Analyzer::InsertNonterminal(Platform::String^ _String)
{
	this->lrGrammar.InsertNonterminal(WStringToString(_String->Data()).c_str());
	this->llGrammar.InsertNonterminal(WStringToString(_String->Data()).c_str());
}

void Kernel::Analyzer::InsertTerminal(Platform::String^ _String)
{
	this->lrGrammar.InsertTerminal(WStringToString(_String->Data()).c_str());
	this->llGrammar.InsertTerminal(WStringToString(_String->Data()).c_str());
}

void Kernel::Analyzer::InsertProduction(Platform::String^ _String)
{
	this->lrGrammar.InsertProduction(WStringToString(_String->Data()).c_str());
	this->llGrammar.InsertProduction(WStringToString(_String->Data()).c_str());
}

void Kernel::Analyzer::SetStartNonterminal(Platform::String^ _String)
{
	this->lrGrammar.SetStartNonterminal(WStringToString(_String->Data()).c_str());
	this->llGrammar.SetStartNonterminal(WStringToString(_String->Data()).c_str());
}

Platform::String ^ Kernel::Analyzer::GetLLAnalysisSheet()
{
	return ref new Platform::String(StringToWString(this->llGrammar.GetAnalysisSheet().c_str()).c_str());
}

Platform::String ^ Kernel::Analyzer::GetSLRDeductions()
{
	return ref new Platform::String(StringToWString(this->lrGrammar.GetSLRDeductions().c_str()).c_str());
}

Platform::String ^ Kernel::Analyzer::GetLRDeductions()
{
	return ref new Platform::String(StringToWString(this->lrGrammar.GetLRDeductions().c_str()).c_str());
}

Platform::String ^ Kernel::Analyzer::GetLRAnalysisSheet()
{
	return ref new Platform::String(StringToWString(this->lrGrammar.GetAnalysisSheet().c_str()).c_str());
}

Platform::String ^ Kernel::Analyzer::LLAnalysis(Platform::String ^ _String)
{
	return ref new Platform::String(StringToWString(this->llGrammar.Analyze(WStringToString(_String->Data()).c_str()).c_str()).c_str());
}

Platform::String ^ Kernel::Analyzer::LRAnalysis(Platform::String ^ _String)
{
	return ref new Platform::String(StringToWString(this->lrGrammar.Analyze(WStringToString(_String->Data()).c_str()).c_str()).c_str());
}
