#pragma once

typedef std::string String, Statement;
typedef std::vector<std::string> Strings;
typedef std::vector<Statement> Statements;

constexpr char TERMINATE = '$';
constexpr char EXTENSION = '\'';

class LRGrammar
{
public:
	enum class TokenType
	{
		EPSILON,
		TERMINAL,
		NONTERMINAL
	};
	typedef TokenType TType;
	typedef std::string Attribute;

	struct Token
	{
		TType type;
		Attribute attr;

		Token() :
			type(TType::EPSILON), attr({ '@' })
		{
		}

		Token(TType _Type, const Attribute& _Attr) :
			type(_Type), attr(_Attr)
		{
		}

		Token(const Token& _Right) :
			type(_Right.type), attr(_Right.attr)
		{
		}

		Token(Token&& _Right) :
			type(_Right.type), attr(_Right.attr)
		{
		}

		Token& operator=(const Token& _Right)
		{
			this->type = _Right.type;
			this->attr = _Right.attr;
			return *this;
		}

		Token& operator=(Token&& _Right)
		{
			this->type = _Right.type;
			this->attr = _Right.attr;
			return *this;
		}

		bool operator==(const Token& _Right) const
		{
			return attr.compare(_Right.attr) == 0;
		}

		bool operator!=(const Token& _Right) const
		{
			return !(*this == _Right);
		}

		bool operator<(const Token& _Right) const
		{
			return attr.compare(_Right.attr) < 0;
		}
	};
	typedef Token Terminal, Nonterminal;
	typedef std::vector<Token> Tokens;
	typedef Tokens Candidate;
	const Terminal Terminate{ TType::TERMINAL, {TERMINATE} };

	struct Production
	{
		Nonterminal nonterminal;
		Candidate candidate;

		Production() :
			nonterminal(), candidate()
		{
		}

		Production(const Token& _Nonterminal, const Candidate& _Candidate) :
			nonterminal(_Nonterminal), candidate(_Candidate)
		{
		}

		Production(const Production& _Right) :
			nonterminal(_Right.nonterminal), candidate(_Right.candidate)
		{
		}

		Production(Production&& _Right) :
			nonterminal(_Right.nonterminal), candidate(_Right.candidate)
		{
		}

		Production& operator=(const Production& _Right)
		{
			this->nonterminal = _Right.nonterminal;
			this->candidate = _Right.candidate;
			return *this;
		}

		Production& operator=(Production&& _Right)
		{
			this->nonterminal = _Right.nonterminal;
			this->candidate = _Right.candidate;
			return *this;
		}

		bool operator==(const Production& _Right) const
		{
			if (this->nonterminal != _Right.nonterminal)
				return false;
			if (this->candidate.size() == _Right.candidate.size())
			{
				for (size_t pos = 0; pos < _Right.candidate.size(); ++pos)
				{
					if (this->candidate[pos] != _Right.candidate[pos])
						return false;
				}
				return true;
			}
			return false;
		}

		bool operator!=(const Production& _Right) const
		{
			return !(*this == _Right);
		}
	};
	typedef std::set<Terminal> Terminals;
	typedef std::set<Nonterminal> Nonterminals;
	typedef std::set<Token> Notes;
	typedef std::list<Production> Productions;

	typedef std::map<Nonterminal, Terminals> Firsts, Follows;
	Firsts firstSet;
	Follows followSet;

	enum class ActionType
	{
		INVAILD,
		SHIFT,
		REDUC,
		ACC
	};
	typedef ActionType AType;
	struct Action
	{
		AType type;
		int nextState;
		Production production;

		Action(int _NextState) :
			type(AType::SHIFT), nextState(_NextState)
		{
		}

		Action(const Production& _Production) :
			type(AType::REDUC), production(_Production)
		{
		}

		Action(AType _Type) :
			type(_Type)
		{
		}

		Action() :
			type(AType::INVAILD)
		{
		}
	};
	typedef std::map<std::pair<int, Terminal>, Action> ActionSheet;
	typedef std::map<std::pair<int, Nonterminal>, int> GotoSheet;

private:
	typedef std::map<std::pair<int, Token>, int> Transfers;

private:
	struct SLRDeduction
	{
		Production production;
		int point;

		SLRDeduction() :
			production(), point(0)
		{
		}

		SLRDeduction(const Production& _Production, int _Point) :
			production(_Production), point(_Point)
		{
		}

		SLRDeduction(const SLRDeduction& _Right) :
			production(_Right.production), point(_Right.point)
		{
		}

		SLRDeduction(SLRDeduction&& _Right) :
			production(_Right.production), point(_Right.point)
		{
		}

		SLRDeduction& operator=(const SLRDeduction& _Right)
		{
			this->production = _Right.production;
			this->point = _Right.point;
			return *this;
		}

		SLRDeduction& operator=(SLRDeduction&& _Right)
		{
			this->production = _Right.production;
			this->point = _Right.point;
			return *this;
		}

		bool operator==(const SLRDeduction& _Right) const
		{
			return this->point == _Right.point && this->production == _Right.production;
		}

		bool operator!=(const SLRDeduction& _Right) const
		{
			return !(*this == _Right);
		}
	};
	typedef std::list<SLRDeduction> SLRDeductions;

	struct SLRState
	{
		int id;
		SLRDeductions deductions;
		const Productions& productions;

		SLRState(const Productions& _Productions, int _Id, const SLRDeductions& _Deductions) :
			productions(_Productions), id(_Id), deductions(_Deductions)
		{
			this->Extend();
		}

		void Extend()
		{
			std::queue<SLRDeduction> unhandledDeductions{ };
			for (const auto& elem : this->deductions)
			{
				unhandledDeductions.push(elem);
			}
			while (!unhandledDeductions.empty())
			{
				SLRDeduction deduction = unhandledDeductions.front();
				unhandledDeductions.pop();

				if (deduction.point < (int)deduction.production.candidate.size()
					&& deduction.production.candidate[deduction.point].type == TType::NONTERMINAL)
				{
					for (const auto& elem : this->productions)
					{
						if (elem.nonterminal == deduction.production.candidate[deduction.point]
							&& std::find_if(this->deductions.begin(), this->deductions.end(),
								[&elem](const SLRDeduction& temp) {
							return temp == SLRDeduction{elem, 0};
						}) == this->deductions.end())
						{
							this->deductions.push_back({ elem, 0 });
							unhandledDeductions.push({ elem, 0 });
						}
					}
				}
			}
		}

		bool operator==(const SLRState& _Right) const
		{
			if (this->deductions.size() != _Right.deductions.size())
				return false;
			for (const auto& elem : _Right.deductions)
			{
				if (std::find(this->deductions.begin(),
					this->deductions.end(),
					elem) == this->deductions.end())
				{
					return false;
				}
			}
			return true;
		}
	};
	typedef std::list<SLRState> SLRStates;
	struct SLRDFA
	{
		SLRStates states;
		Transfers transfers;

		SLRDFA()
		{
		}

		SLRDFA(const SLRState& _Start) :
			states({_Start}),
			transfers()
		{
		}
	};

private:
	struct LRDeduction
	{
		Production production;
		int point;
		Terminal tail;

		LRDeduction() :
			production(), point(0),
			tail()
		{
		}

		LRDeduction(const Production& _Production, int _Point, const Terminal& _Tail) :
			production(_Production), point(_Point),
			tail(_Tail)
		{
		}

		LRDeduction(const LRDeduction& _Right) :
			production(_Right.production), point(_Right.point),
			tail(_Right.tail)
		{
		}

		LRDeduction(LRDeduction&& _Right) :
			production(_Right.production), point(_Right.point),
			tail(_Right.tail)
		{
		}

		LRDeduction& operator=(const LRDeduction& _Right)
		{
			this->production = _Right.production;
			this->point = _Right.point;
			this->tail = _Right.tail;
			return *this;
		}

		LRDeduction& operator=(LRDeduction&& _Right)
		{
			this->production = _Right.production;
			this->point = _Right.point;
			this->tail = _Right.tail;
			return *this;
		}

		bool operator==(const LRDeduction& _Right) const
		{
			return this->point == _Right.point && this->production == _Right.production && this->tail == _Right.tail;
		}

		bool operator!=(const LRDeduction& _Right) const
		{
			return !(*this == _Right);
		}
	};
	typedef std::list<LRDeduction> LRDeductions;

	struct LRState
	{
		int id;
		LRDeductions deductions;

		const Productions& productions;
		Firsts& firstSet;

		LRState(int _Id, const LRDeductions& _Deductions, const Productions& _Productions, Firsts& _FirstSet) :
			id(_Id), deductions(_Deductions),
			productions(_Productions), firstSet(_FirstSet)
		{
			this->Extend();
		}

		void Extend()
		{
			std::queue<LRDeduction> unhandledDeductions{ };
			for (const auto& elem : this->deductions)
			{
				unhandledDeductions.push(elem);
			}
			while (!unhandledDeductions.empty())
			{
				LRDeduction deduction = unhandledDeductions.front();
				unhandledDeductions.pop();

				if (deduction.point < (int)deduction.production.candidate.size()
					&& deduction.production.candidate[deduction.point].type == TType::NONTERMINAL)
				{
					Candidate newCandidate{ deduction.production.candidate.begin() + deduction.point + 1, deduction.production.candidate.end() };
					newCandidate.push_back(deduction.tail);
					Terminals newTerminals = LRGrammar::GetFirst(newCandidate, firstSet);

					for (const auto& production : this->productions)
					{
						for (const auto& terminal : newTerminals)
						{
							if (production.nonterminal == deduction.production.candidate[deduction.point]
								&& std::find_if(this->deductions.begin(), this->deductions.end(),
									[&production, &terminal](const LRDeduction& temp) {

								return temp == LRDeduction{ production, 0, terminal };
							}) == this->deductions.end())
							{
								this->deductions.push_back({ production, 0, terminal });
								unhandledDeductions.push({ production, 0, terminal });
							}
						}
					}
				}
			}
		}

		bool operator==(const LRState& _Right) const
		{
			if (this->deductions.size() != _Right.deductions.size())
				return false;
			for (const auto& elem : _Right.deductions)
			{
				if (std::find(this->deductions.begin(),
					this->deductions.end(),
					elem) == this->deductions.end())
				{
					return false;
				}
			}
			return true;
		}
	};
	typedef std::list<LRState> LRStates;
	struct LRDFA
	{
		LRStates states;
		Transfers transfers;

		LRDFA()
		{
		}

		LRDFA(const LRState& _Start) :
			states({ _Start }),
			transfers()
		{
		}
	};

public:
	Notes GetAllNotes() const;
	Productions GetAllProductions() const;
	Nonterminal GetStartNonterminal() const;

	bool InsertTerminal(const char _Statement[]);
	bool InsertNonterminal(const char _Statement[]);
	bool InsertProduction(const char _Statement[]);
	bool InsertProduction(const Production& _Production);
	bool SetStartNonterminal(const char _Statement[]);

	Firsts GetFirstSet() const;
	Follows GetFollowSet() const;

public:
	std::string GetSLRDeductions();
	std::string GetLRDeductions();
	std::string GetAnalysisSheet() const;
	std::string Analyze(const char _Word[]) const;

private:
	Notes notes;
	Productions productions;
	Nonterminal start;

	int dfaStatesCnt;
	ActionSheet actions;
	GotoSheet gotos;
	bool isExtend;

private:
	void Extend();
	Firsts WorkOutFirsts() const;
	Follows WorkOutFollows(Firsts& _First) const;

	SLRDFA BuildSLRDFA();
	LRDFA BuildLRDFA();

public:
	static LRGrammar::Terminals GetFirst(const LRGrammar::Candidate& _Candidate, LRGrammar::Firsts& _Firsts);
	static LRGrammar::Terminals GetFollow(
		const LRGrammar::Nonterminal& _Dest,
		const LRGrammar::Productions& _Productions,
		LRGrammar::Firsts& _Firsts,
		LRGrammar::Follows& _Follows,
		std::vector<LRGrammar::Nonterminal>& _Sours,
		std::map<LRGrammar::Nonterminal, bool>& _DoneList,
		const LRGrammar::Token& _Start
	);
};