#include "pch.h"
#include "lrgrammar.h"

/// <summary>
/// �Ƴ�����еĿհ��ַ��Լ��Ʊ��
/// </summary>
/// <param name="statement">����������</param>
/// <returns>��ʽ�����</returns>
static Statement RemoveSpace(const char _Statement[])
{
	Statement modifiedStatement{ };
	int length = (int)std::strlen(_Statement);
	std::for_each(_Statement, _Statement + length,
		[&modifiedStatement](char ch) {
		if (ch != ' ' && ch != '\t')
			modifiedStatement.push_back(ch);
	});
	return modifiedStatement;
}

/// <summary>
/// �����еķǷ��ַ��ϼ�
/// </summary>
/// <param name="ch"></param>
/// <returns></returns>
static bool InvalidCharacter(char _Ch)
{
	return (_Ch == ' ') || (_Ch == '\t') || (_Ch == '.') || (_Ch == '#')
		|| (_Ch == '\0') || (_Ch == '\n') || (_Ch == '\r') || (_Ch == '|');
}

/// <summary>
/// ��������䰴���ض��ַ����зָ�Ϊ���ɸ��Ӿ�
/// </summary>
/// <param name="statement"></param>
/// <param name="separator"></param>
/// <returns></returns>
static Statements SplitStringBy(const Statement& _Statement, char _Separator)
{
	Statements result{ };
	Statement substatement{ };
	std::for_each(_Statement.begin(), _Statement.end(),
		[&result, &substatement, _Separator](char ch) {
		if (ch == _Separator)
		{
			if (substatement.size() != 0)
			{
				result.push_back(substatement);
				substatement.clear();
			}
		}
		else
		{
			substatement.push_back(ch);
		}
	});
	if (substatement.size() > 0)
		result.push_back(substatement);
	return result;
}

void LRGrammar::Extend()
{
	this->isExtend = true;
	Attribute newAttr = this->start.attr;
	do
	{
		newAttr += EXTENSION;
	} while (std::find(this->notes.begin(),
		this->notes.end(), Token{ TType::NONTERMINAL, newAttr }) != this->notes.end());
	this->notes.insert({ TType::NONTERMINAL, newAttr });
	this->productions.push_back({ {TType::NONTERMINAL, newAttr}, {this->start} });
	this->start = { TType::NONTERMINAL, newAttr };
}

/// <summary>
/// �ݹ�ɼ�ĳ�����ս����FIRST��
/// </summary>
/// <param name="candidate"></param>
/// <param name="firstSet"></param>
/// <returns></returns>
LRGrammar::Terminals LRGrammar::GetFirst(const LRGrammar::Candidate& _Candidate, LRGrammar::Firsts& _Firsts)
{
	LRGrammar::Terminals first;
	LRGrammar::Token front = _Candidate.front();
	if (front.type == LRGrammar::TType::TERMINAL)
	{
		first.insert(front);
	}
	else if (front.type == LRGrammar::TType::NONTERMINAL)
	{
		if (_Firsts[front].size() > 0)
		{ // ��һ�����ս����FIRST����֪ ֱ�Ӻϲ�
			bool epsilon = false;
			std::for_each(_Firsts[front].begin(), _Firsts[front].end(),
				[&epsilon, &first](const LRGrammar::Terminal& elem) {
				if (elem.type == TType::EPSILON)
					epsilon = true;
				else
					first.insert(elem); // �ϲ��ǿ�Ԫ��
			});
			if (epsilon)
			{
				/* �ɵ��� */
				LRGrammar::Candidate newCandidate = _Candidate;
				newCandidate.erase(newCandidate.begin());
				if (newCandidate.size() == 0)
				{
					first.insert(LRGrammar::Token{ }); // �޺���� ����epsilon
				}
				else
				{
					LRGrammar::Terminals more = GetFirst(newCandidate, _Firsts);
					if (more.size() == 0)
					{
						return { }; // �ݹ����������FIRST���Ƿ���
					}
					else
					{ // FIRST���Ϸ�
						for (const auto& elem : more)
						{
							first.insert(elem);
						}
					}
				}
			}
		}
		else
		{
			// ��ʼ���ս����FIRST��δ֪ ��ǰ���ս��Ҳ�޷�������������޵ݹ飩
			// ���ؿռ��Ա����÷��ս����FIRST���Ƿ�
			return { };
		}
	}
	else if (front.type == LRGrammar::TType::EPSILON)
	{
		first.insert(LRGrammar::Terminal{});
	}
	return first;
}

/// <summary>
/// �ݹ�ɼ�ĳ�����ս����FOLLOW��
/// </summary>
/// <param name="dest">������ս��</param>
/// <param name="productions">����ʽ����</param>
/// <param name="firstSet"></param>
/// <param name="followSet"></param>
/// <param name="sours">�Ѿ����ʹ��ķ��ս��</param>
/// <param name="doneList">FOLLOW����֪�ķ��ս��</param>
/// <returns></returns>
LRGrammar::Terminals LRGrammar::GetFollow(
	const LRGrammar::Nonterminal& _Dest,
	const LRGrammar::Productions& _Productions,
	LRGrammar::Firsts& _Firsts,
	LRGrammar::Follows& _Follows,
	std::vector<LRGrammar::Nonterminal>& _Sours,
	std::map<LRGrammar::Nonterminal, bool>& _DoneList,
	const LRGrammar::Token& _Start
)
{
	LRGrammar::Terminals follow;
	LRGrammar::Candidate candidate;
	for (const auto& production : _Productions)
	{
		LRGrammar::Terminals more;
		LRGrammar::Candidate::const_iterator next = production.candidate.begin();
		while ((next = std::find(next, production.candidate.end(), _Dest)) != production.candidate.end())
		{ // ������ս�������ڸò���ʽ��
			++next;
			bool epsilon = false;
			if (next != production.candidate.end())
			{
				LRGrammar::Candidate candidate{ next, production.candidate.end() };
				more = GetFirst(candidate, _Firsts);
				for (const auto& elem : more)
				{
					if (elem.type == TType::EPSILON)
						epsilon = true;
					else
						follow.insert(elem);
				}
			}
			if (epsilon
				|| next == production.candidate.end())
			{
				/* �ϲ�����ʽ�󲿵�FOLLOW�� */
				if (_DoneList[production.nonterminal])
				{ // �󲿵�FOLLOW����֪
					for (const auto& elem : _Follows[production.nonterminal])
					{
						follow.insert(elem);
					}
				}
				else if (production.nonterminal != _Sours[0]
					&& production.nonterminal != _Dest)
				{   // δ֪������󲿷��ս�����ǵ�һ��������ս�����뵱ǰ������ս����ͬʱ�Ž��еݹ�
					// �Ա������޵ݹ�
					if (std::find(_Sours.begin() + 1, _Sours.end(), production.nonterminal)
						== _Sours.end())
					{ // ���÷��շ�δ���ݹ���� ������ѷ��ʼ���
						_Sours.push_back(production.nonterminal);
						more = GetFollow(
							production.nonterminal,
							_Productions,
							_Firsts, _Follows,
							_Sours, _DoneList,
							_Start
						);
						if (_Sours.size() == 0)
							return { }; // ������ʧ��
						else
						{
							_Sours.pop_back();
							if (production.nonterminal == _Start)
								follow.insert(
									LRGrammar::Token{ LRGrammar::TType::TERMINAL, {TERMINATE} }
							);
							for (const auto& elem : more)
								follow.insert(elem);
						}
					}
					else
					{   // �������޵ݹ�
						// ���ѷ��ʼ�����Ϊ���Ա�־��ǰ������ʧ��
						_Sours.clear();
						return { };
					}
				}
			}
		}
	}
	return follow;
}

LRGrammar::Firsts LRGrammar::WorkOutFirsts() const
{
	Firsts firstSet;
	Productions oldProductions
		= this->productions;
	bool isDone;
	int count = 0;
	do
	{
		std::for_each(this->notes.begin(), this->notes.end(),
			[&firstSet, &isDone, &oldProductions](const Token& nonterminal) {
			if (nonterminal.type == TType::NONTERMINAL)
			{
				/* �ɼ�FIRST�� */
				Terminals first;
				std::for_each(oldProductions.begin(), oldProductions.end(),
					[&firstSet, &nonterminal, &first](const Production& production) {
					if (production.nonterminal == nonterminal)
					{
						Terminals more = GetFirst(production.candidate, firstSet);
						for (const auto& elem : more)
							first.insert(elem);
					}
				});

				for (const auto& elem : first)
				{
					if (firstSet[nonterminal].find(elem) == firstSet[nonterminal].end())
					{
						firstSet[nonterminal].insert(elem);
					}
				}
			}
		});
	} while (++count <= (int)this->notes.size() * (int)this->notes.size());
	return firstSet;
}

LRGrammar::Follows LRGrammar::WorkOutFollows(Firsts& _First) const
{
	Follows followSet;
	followSet[this->start].insert(this->Terminate);

	std::map<Token, bool> doneList;
	int count = 0;
	do
	{
		std::for_each(this->notes.begin(), this->notes.end(),
			[&doneList, this, &_First, &followSet](const Token& nonterminal) {
			if (nonterminal.type == TType::NONTERMINAL
				&& doneList[nonterminal] == false)
			{
				/* �ɼ�FOLLOW�� */
				std::vector<Nonterminal> sours{ nonterminal };
				Terminals follow
					= GetFollow(
						nonterminal,
						this->GetAllProductions(),
						_First, followSet,
						sours, doneList,
						this->GetStartNonterminal()
					);
				for (const auto& elem : follow)
				{
					followSet[nonterminal].insert(elem);
					doneList[nonterminal] = true;
				}
			}
		});

	} while (++count <= 2 * (int)this->notes.size());
	return followSet;
}

LRGrammar::SLRDFA LRGrammar::BuildSLRDFA()
{
	Productions::const_iterator	startProduction =
		std::find_if(this->productions.begin(),
			this->productions.end(),
			[this](const Production& production) {
		return production.nonterminal == this->GetStartNonterminal();
	});
	if (startProduction != this->productions.end())
	{
		SLRDFA slrDfa{ {this->productions, 0, {{*startProduction, 0}}} };

		std::queue<SLRState> unhandledStates{ { slrDfa.states.front() } };
		while (!unhandledStates.empty())
		{
			SLRState currentState = unhandledStates.front();
			unhandledStates.pop();

			/* ���� */
			for (const auto& token : notes)
			{
				SLRDeductions newDeductions;
				for (const auto& deduction : currentState.deductions)
				{
					if (deduction.point < (int)deduction.production.candidate.size()
						&& deduction.production.candidate[deduction.point] == token)
					{ /* ���ƽ���Ŀ */
						newDeductions.push_back({ deduction.production, deduction.point + 1 });
					}
				}
				SLRStates::const_iterator existState = std::find_if(slrDfa.states.begin(),
					slrDfa.states.end(),
					[&newDeductions, this](const SLRState& state) {
					return state == SLRState{ this->productions, 0, newDeductions };
				});
				if (newDeductions.size() == 0)
					continue;

				if (existState == slrDfa.states.end())
				{
					unhandledStates.push({ this->productions,(int)slrDfa.states.size(), newDeductions });

					slrDfa.transfers[std::pair<int, Terminal>{currentState.id, token}] = (int)slrDfa.states.size();
					slrDfa.states.push_back({ this->productions,(int)slrDfa.states.size(), newDeductions });
				}
				else
				{
					slrDfa.transfers[std::pair<int, Terminal>{currentState.id, token}] = existState->id;
				}
			}
		}
		return slrDfa;
	}
	return { };
}

LRGrammar::LRDFA LRGrammar::BuildLRDFA()
{
	Productions::const_iterator	startProduction =
		std::find_if(this->productions.begin(),
			this->productions.end(),
			[this](const Production& production) {
		return production.nonterminal == this->GetStartNonterminal();
	});
	if (startProduction != this->productions.end())
	{
		LRDFA lrDfa{ {0, {{*startProduction, 0, this->Terminate }}, this->productions, this->firstSet} };

		std::queue<LRState> unhandledStates{ { lrDfa.states.front() } };
		while (!unhandledStates.empty())
		{
			LRState currentState = unhandledStates.front();
			unhandledStates.pop();

			/* ���� */
			for (const auto& token : notes)
			{
				LRDeductions newDeductions;
				for (const auto& deduction : currentState.deductions)
				{
					if (deduction.point < (int)deduction.production.candidate.size()
						&& deduction.production.candidate[deduction.point] == token)
					{ /* ���ƽ���Ŀ */
						newDeductions.push_back({ deduction.production, deduction.point + 1, deduction.tail });
					}
				}
				LRStates::const_iterator existState = std::find_if(lrDfa.states.begin(),
					lrDfa.states.end(),
					[&newDeductions, this](const LRState& state) {
					return state == LRState{ 0, newDeductions, this->productions, this->firstSet };
				});
				if (newDeductions.size() == 0)
					continue;

				if (existState == lrDfa.states.end())
				{
					unhandledStates.push({ (int)lrDfa.states.size(), newDeductions, this->productions, this->firstSet });

					lrDfa.transfers[std::pair<int, Terminal>{currentState.id, token}] = (int)lrDfa.states.size();
					lrDfa.states.push_back({ (int)lrDfa.states.size(), newDeductions, this->productions, this->firstSet });
				}
				else
				{
					lrDfa.transfers[std::pair<int, Terminal>{currentState.id, token}] = existState->id;
				}
			}
		}
		return lrDfa;
	}
	return { };
}

std::string LRGrammar::GetSLRDeductions()

{
	if (!this->isExtend)
		this->Extend();

	this->firstSet = this->WorkOutFirsts();
	this->followSet = this->WorkOutFollows(this->firstSet);

	SLRDFA slrDfa = this->BuildSLRDFA();
	this->dfaStatesCnt = (int)slrDfa.states.size();

	std::string description = { };
	for (const auto& elem : slrDfa.states)
	{
		for (const auto& deduction : elem.deductions)
		{
			description += deduction.production.nonterminal.attr + "��";
			for (int pos = 0; pos < (int)deduction.production.candidate.size(); ++pos)
			{
				if (pos == deduction.point)
					description += "��";
				if (deduction.production.candidate[pos].type != TType::EPSILON)
					description += deduction.production.candidate[pos].attr;
			}
			if (deduction.point == (int)deduction.production.candidate.size())
				description += "��";
			description += '\n';
		}
		description += '\t';
	}

	/* �ƽ���Ŀ */
	for (const auto& elem : slrDfa.transfers)
	{
		if (elem.first.second.type == TType::TERMINAL)
		{ /* Action�� */
			this->actions[elem.first] = { elem.second };
		}
		else if (elem.first.second.type == TType::NONTERMINAL)
		{ /* Goto�� */
			this->gotos[elem.first] = elem.second;
		}
	}
	/* ��Լ�ͽ�����Ŀ */
	for (const auto& elem : slrDfa.states)
	{
		for (const auto& deduction : elem.deductions)
		{
			if (deduction.production.nonterminal == this->start
				&& deduction.point == deduction.production.candidate.size())
			{ /* ������Ŀ */
				this->actions[std::pair<int, Terminal>{elem.id, this->Terminate}] = { AType::ACC };
			}
			else if (deduction.point == deduction.production.candidate.size())
			{ /* ��Լ��Ŀ */
				Terminals terminals = this->followSet[deduction.production.nonterminal];
				for (const auto& terminal : terminals)
				{
					if (this->actions.find(std::pair<int, Terminal>{elem.id, terminal}) != this->actions.end())
					{
						this->dpactions[std::pair<int, Terminal>{elem.id, terminal}].push_back(deduction.production);
					}
					else
					{
						this->actions[std::pair<int, Terminal>{elem.id, terminal}] = deduction.production;
					}
				}
			}
			else if (deduction.production.candidate.front().type == TType::EPSILON)
			{ /* ��Լ��Ŀ */
				Terminals terminals = this->followSet[deduction.production.nonterminal];
				for (const auto& terminal : terminals)
				{
					if (this->actions.find(std::pair<int, Terminal>{elem.id, terminal}) != this->actions.end())
					{
						this->dpactions[std::pair<int, Terminal>{elem.id, terminal}].push_back(deduction.production);
					}
					else
					{
						this->actions[std::pair<int, Terminal>{elem.id, terminal}] = deduction.production;
					}
				}
			}
		}
	}
	return description;
}

std::string LRGrammar::GetLRDeductions()
{
	if (!this->isExtend)
		this->Extend();

	this->firstSet = this->WorkOutFirsts();
	this->followSet = this->WorkOutFollows(this->firstSet);

	LRDFA lrDfa = this->BuildLRDFA();
	this->dfaStatesCnt = (int)lrDfa.states.size();

	std::string description = { };
	for (const auto& elem : lrDfa.states)
	{
		std::list<SLRDeduction> isPrint{ };
		for (LRDeductions::const_iterator any = elem.deductions.begin();
			any != elem.deductions.end(); ++any)
		{
			if (std::find(isPrint.begin(), isPrint.end(), SLRDeduction{ any->production, any->point })
				== isPrint.end())
			{
				isPrint.push_back({ any->production, any->point });

				description += any->production.nonterminal.attr + "��";
				for (int pos = 0; pos < (int)any->production.candidate.size(); ++pos)
				{
					if (pos == any->point)
						description += "��";
					if (any->production.candidate[pos].type != TType::EPSILON)
						description += any->production.candidate[pos].attr;
				}
				if (any->point == any->production.candidate.size())
					description += "��";
				description += "��";
				description += any->tail.attr;
				LRDeductions::const_iterator more = any;
				while (++more != elem.deductions.end())
				{
					if (SLRDeduction{ any->production, any->point } == SLRDeduction{ more->production, more->point })
					{
						description += " ";
						description += more->tail.attr;
					}
				}
				description += "\n";
			}
		}
		description += '\t';
	}


	/* �ƽ���Ŀ */
	for (const auto& elem : lrDfa.transfers)
	{
		if (elem.first.second.type == TType::TERMINAL)
		{ /* Action�� */
			this->actions[elem.first] = { elem.second };
		}
		else if (elem.first.second.type == TType::NONTERMINAL)
		{ /* Goto�� */
			this->gotos[elem.first] = elem.second;
		}
	}
	/* ��Լ�ͽ�����Ŀ */
	for (const auto& elem : lrDfa.states)
	{
		for (const auto& deduction : elem.deductions)
		{
			if (deduction.production.nonterminal == this->start
				&& deduction.point == deduction.production.candidate.size())
			{ /* ������Ŀ */
				this->actions[std::pair<int, Terminal>{elem.id, this->Terminate}] = { AType::ACC };
			}
			else if (deduction.point == deduction.production.candidate.size())
			{ /* ��Լ��Ŀ */
				if (this->actions.find(std::pair<int, Terminal>{elem.id, deduction.tail}) != this->actions.end())
				{
					this->dpactions[std::pair<int, Terminal>{elem.id, deduction.tail}].push_back(deduction.production);
				}
				else
				{
					this->actions[std::pair<int, Terminal>{elem.id, deduction.tail}] = deduction.production;
				}
			}
			else if (deduction.production.candidate.front().type == TType::EPSILON)
			{ /* ��Լ��Ŀ */
				if (this->actions.find(std::pair<int, Terminal>{elem.id, deduction.tail}) != this->actions.end())
				{
					this->dpactions[std::pair<int, Terminal>{elem.id, deduction.tail}].push_back(deduction.production);
				}
				else
				{
					this->actions[std::pair<int, Terminal>{elem.id, deduction.tail}] = deduction.production;
				}
			}
		}
	}
	return description;
}

std::string LRGrammar::GetAnalysisSheet() const
{
	Terminals terminals{ {this->Terminate} };
	std::for_each(this->notes.begin(), this->notes.end(),
		[&terminals](const Token& token) {
		if (token.type == TType::TERMINAL)
			terminals.insert(token);
	});

	Nonterminals nonterminals{ };
	std::for_each(this->notes.begin(), this->notes.end(),
		[&nonterminals](const Token& token) {
		if (token.type == TType::NONTERMINAL)
			nonterminals.insert(token);
	});
	nonterminals.erase(this->start);

	std::string description = { };
	description += "     \t";
	for (const auto& elem : terminals)
	{
		description += elem.attr;
		description += '\t';
	}
	for (const auto& elem : nonterminals)
	{
		description += elem.attr;
		description += '\t';
	}
	description += '\n';

	for (int state = 0; state < this->dfaStatesCnt; ++state)
	{
		char number[BUFSIZ];
		sprintf(number, "%d\t", state);
		description += number;
		for (const auto& elem : terminals)
		{
			if (this->actions.find({ state, elem }) != this->actions.end())
			{
				Action act = this->actions.at({ state, elem });
				if (act.type == AType::SHIFT)
				{
					char shift[BUFSIZ];
					sprintf(shift, "Shift %d", act.nextState);
					description += shift;
				}
				else if (act.type == AType::REDUC)
				{
					description += "Reduce by ";
					description += act.production.nonterminal.attr + "��";
					for (const auto& token : act.production.candidate)
					{
						if (token.type == TType::EPSILON
							|| token.attr.compare({ '@' }) == 0)
							description += "��";
						else
							description += token.attr;
					}
				}
				else
				{
					description += "ACC";
				}

				if (this->dpactions.find({ state, elem }) != this->dpactions.end())
				{
					const std::vector<Action>& dpacts = this->dpactions.at({ state, elem });
					for (const auto& act : dpacts)
					{
						description += " �� ";
						if (act.type == AType::SHIFT)
						{
							char shift[BUFSIZ];
							sprintf(shift, "Shift %d", act.nextState);
							description += shift;
						}
						else if (act.type == AType::REDUC)
						{
							description += "Reduce by ";
							description += act.production.nonterminal.attr + "��";
							for (const auto& token : act.production.candidate)
							{
								if (token.type == TType::EPSILON
									|| token.attr.compare({ '@' }) == 0)
									description += "��";
								else
									description += token.attr;
							}
						}
						else
						{
							description += "ACC";
						}
					}
				}
			}
			else
			{
				description += " ";
			}
			description += '\t';
		}

		for (const auto& elem : nonterminals)
		{
			if (this->gotos.find({ state, elem }) != this->gotos.end())
			{
				char next[BUFSIZ];
				sprintf(next, "%d", this->gotos.at({ state, elem }));
				description += next;
			}
			else
			{
				description += " ";
			}
			description += '\t';
		}
		description += "\n";
	}
	return description;
}

std::string LRGrammar::Analyze(const char _Word[]) const
{
	Statements words
		= SplitStringBy(_Word, ' ');
	/* ������������� */
	Tokens tokens{ };
	Token terminal{ };
	for (const auto& elem : words)
	{
		if (elem.compare(" ") != 0)
		{
			terminal.attr = elem;
			tokens.push_back(terminal);
		}
	}
	tokens.push_back(this->Terminate);
	tokens.erase(std::find(tokens.begin(), tokens.end(), this->Terminate) + 1,
		tokens.end());

	std::string description = { };
	description += "Step\tState\tToken\tInput\tOutput\n";

	std::vector<int> stateStack{ };
	stateStack.push_back(0);

	std::vector<Token> tokenStack{ };
	tokenStack.push_back(this->Terminate);

	Tokens::const_iterator input = tokens.begin();
	int step = 1;
	char buf[BUFSIZ] = { 0x0 };
	while (input != tokens.end())
	{
		sprintf(buf, "%d", step);
		step++;
		description += buf;
		description += '\t';

		/* ״̬ջ */
		for (const auto& state : stateStack)
		{
			sprintf(buf, "%d ", state);
			description += buf;
		}
		description += '\t';

		/* ����ջ */
		for (const auto& token : tokenStack)
			description += token.attr + ' ';
		description += '\t';

		/* ���� */
		for (Tokens::const_iterator rest = input; rest != tokens.end(); ++rest)
			description += rest->attr + ' ';
		description += '\t';

		if (this->actions.find(std::pair<int, Terminal>{ stateStack.back(), *input })
			!= this->actions.end())
		{
			Action action = this->actions.at(std::pair<int, Terminal>{ stateStack.back(), *input });
			if (action.type == AType::SHIFT)
			{
				stateStack.push_back(action.nextState);
				tokenStack.push_back(*input);

				++input;

				sprintf(buf, "Shift %d", action.nextState);
				description += buf;
				description += '\n';
			}
			else if (action.type == AType::REDUC)
			{
				if (stateStack.size() <= action.production.candidate.size()
					&& action.production.candidate.front().type != TType::EPSILON)
				{
					/* ��� */
					description += "��Լ����\n";
					return description;
				}
				else
				{
					if (action.production.candidate.front().type != TType::EPSILON)
					{
						for (size_t cnt = 0; cnt < action.production.candidate.size(); ++cnt)
						{
							stateStack.pop_back();
							tokenStack.pop_back();
						}
					}

					if (this->gotos.find(std::pair<int, Nonterminal>{ stateStack.back(), action.production.nonterminal }) != this->gotos.end())
					{
						stateStack.push_back(this->gotos.at(std::pair<int, Nonterminal>{ stateStack.back(), action.production.nonterminal }));
						tokenStack.push_back(action.production.nonterminal);

						/* ��� */
						description += "Reduce by ";
						description += action.production.nonterminal.attr + "��";

						for (const auto& elem : action.production.candidate)
						{
							if (elem.type == TType::EPSILON)
								description += "��";
							else
								description += elem.attr;
						}
						description += '\n';
					}
					else
					{
						/* ��� */
						description += "��ת����\n";
						return description;
					}
				}
			}
			else if (action.type == AType::ACC)
			{
				/* ��� */
				description += "ACC\n";
				return description;
			}
			else
			{
				/* ��� */
				description += "δ֪����\n";
				return description;
			}
		}
		else
		{
			/* ��� */
			description += "δ֪����\n";
			return description;
		}
	}

	return description;
}

LRGrammar::Notes LRGrammar::GetAllNotes() const
{
	return this->notes;
}

LRGrammar::Productions LRGrammar::GetAllProductions() const
{
	return this->productions;
}

LRGrammar::Nonterminal LRGrammar::GetStartNonterminal() const
{
	return this->start;
}

bool LRGrammar::InsertTerminal(const char _Statement[])
{
	Statement modifiedStatement
		= RemoveSpace(_Statement);

	Attribute newAttr{ };
	int pos = 0;
	while (!InvalidCharacter(modifiedStatement[pos]))
		newAttr.push_back(modifiedStatement[pos++]);
	if (this->notes.find({ TType::TERMINAL, newAttr }) == this->notes.end())
	{
		this->notes.insert({ TType::TERMINAL, newAttr });
		return true;
	}
	return false; // �����ظ�����
}

bool LRGrammar::InsertNonterminal(const char _Statement[])
{
	Statement modifiedStatement
		= RemoveSpace(_Statement);

	Attribute newAttr{ };
	int pos = 0;
	while (!InvalidCharacter(modifiedStatement[pos]))
		newAttr.push_back(modifiedStatement[pos++]);
	if (this->notes.find({ TType::NONTERMINAL, newAttr }) == this->notes.end())
	{
		this->notes.insert({ TType::NONTERMINAL, newAttr });
		return true;
	}
	return false; // �����ظ�����
}

bool LRGrammar::InsertProduction(const char _Statement[])
{
	Statement modifiedStatement
		= RemoveSpace(_Statement);

	Statements nonterminalAndCandidates
		= SplitStringBy(modifiedStatement, '#');
	if (nonterminalAndCandidates.size() != 2)
		return false; // �Ƿ����

	Attribute newAttr{ };
	int pos = 0;
	while (!InvalidCharacter(nonterminalAndCandidates[0][pos]))
		newAttr.push_back(nonterminalAndCandidates[0][pos++]);
	Notes::const_iterator note = this->notes.find({ TType::NONTERMINAL, newAttr });
	if (note == this->notes.end()
		|| note->type != TType::NONTERMINAL)
	{
		return false;
	}

	Statements candidates
		= SplitStringBy(nonterminalAndCandidates[1], '|');
	if (candidates.size() == 0)
		return false; // �޺Ϸ���ѡʽ

	/* ����Ϸ�����ʽ */
	volatile bool failed = false;
	Production production{ {TType::NONTERMINAL, newAttr}, { } };
	std::for_each(candidates.begin(), candidates.end(),
		[this, &production, &failed](const Statement& statement) {
		if (!failed)
		{
			Statements tokens
				= SplitStringBy(statement, '.');
			if (tokens.size() == 0)
				return; // �޺Ϸ�����

			/* ����Ϸ���ѡʽ */
			Candidate candidate;
			std::for_each(tokens.begin(), tokens.end(),
				[this, &candidate, &failed](const Statement& statement) {
				if (!failed)
				{
					int pos = 0;
					Attribute newAttr{ };
					while (!InvalidCharacter(statement[pos]))
						newAttr.push_back(statement[pos++]);

					Notes allNotes = this->GetAllNotes();
					Notes::const_iterator exist =
						allNotes.find({ TType::TERMINAL, newAttr });
					if (exist != allNotes.end())
					{
						candidate.push_back({ exist->type, newAttr });
					}
					else if (newAttr.compare({ '@' }) == 0)
					{	// ��ѡʽΪEpsilon
						candidate.push_back({ });
					}
					else
					{
						failed = true; // �Ƿ����� �ò���ʽ����
					}
				}
			});

			/* ����Ϸ�����ʽ */
			if (!failed && candidate.size() != 0)
			{
				production.candidate = candidate;
				this->InsertProduction(production);
			}
		}
	});
	return !failed;
}

bool LRGrammar::InsertProduction(const Production& _Production)
{
	if (std::find(this->productions.begin(), this->productions.end(), _Production)
		== this->productions.end())
	{
		this->productions.push_back(_Production);
		return true;
	}
	return false; // �����ظ�����
}

bool LRGrammar::SetStartNonterminal(const char _Statement[])
{
	Statement modifiedStatement
		= RemoveSpace(_Statement);

	Attribute oldAtrr{ };
	int pos = 0;
	while (!InvalidCharacter(modifiedStatement[pos]))
		oldAtrr.push_back(modifiedStatement[pos++]);
	Notes::const_iterator startNonterminal = this->notes.find({ TType::NONTERMINAL, oldAtrr });
	if (startNonterminal == this->notes.end()
		|| startNonterminal->type != TType::NONTERMINAL)
	{
		return false;
	}
	this->start = *startNonterminal;
	return true;
}

LRGrammar::Firsts LRGrammar::GetFirstSet() const
{
	return this->firstSet;
}

LRGrammar::Follows LRGrammar::GetFollowSet() const
{
	return this->followSet;
}


