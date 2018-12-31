#pragma once

namespace Kernel
{
    public ref class Analyzer sealed
    {
    public:
		Analyzer();

		void InsertNonterminal(Platform::String^ _String);
		void InsertTerminal(Platform::String^ _String);
		void InsertProduction(Platform::String^ _String);
		void SetStartNonterminal(Platform::String^ _String);

		Platform::String^ GetLLAnalysisSheet();

		Platform::String^ GetSLRDeductions();
		Platform::String^ GetLRDeductions();
		Platform::String^ GetLRAnalysisSheet();

		Platform::String^ LLAnalysis(Platform::String^ _String);
		Platform::String^ LRAnalysis(Platform::String^ _String);

	private:
		LRGrammar lrGrammar;
		LLGrammar llGrammar;
    };
}
